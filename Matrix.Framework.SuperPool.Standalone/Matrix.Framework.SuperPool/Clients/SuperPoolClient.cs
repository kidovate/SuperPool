// -----
// Copyright 2010 Deyan Timnev
// This file is part of the Matrix Platform (www.matrixplatform.com).
// The Matrix Platform is free software: you can redistribute it and/or modify it under the terms of the GNU Lesser General Public License as published by the Free Software Foundation, 
// either version 3 of the License, or (at your option) any later version. The Matrix Platform is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; 
// without even the implied warranty of  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Lesser General Public License for more details.
// You should have received a copy of the GNU Lesser General Public License along with the Matrix Platform. If not, see http://www.gnu.org/licenses/lgpl.html
// -----
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Matrix.Common.Core;
using Matrix.Common.Core.Collections;
using Matrix.Common.Core.Identification;
using Matrix.Framework.MessageBus.Clients;
using Matrix.Framework.MessageBus.Core;
using Matrix.Framework.SuperPool.Call;
using Matrix.Framework.SuperPool.Core;
using Matrix.Framework.SuperPool.DynamicProxy;
using Matrix.Framework.SuperPool.Subscription;

#if Matrix_Diagnostics
using Matrix.Common.Diagnostics;
#endif

namespace Matrix.Framework.SuperPool.Clients
{
    /// <summary>
    /// Client extends the message bus client invocator, by providing
    /// integration to the SuperPool mechanism (converting interface 
    /// calls to messages and back to calls); so in fact messages
    /// are generic and contain all the info needed for the call
    /// upon arival.
    /// </summary>
    public class SuperPoolClient : ActiveInvocatorClient, ISuperPoolClient
    {
        object _syncRoot = new object();

        public const int GarbageCollectorIntervalMs = 5000;

        System.Threading.Timer _garbageCollectorTimer;

        HotSwapList<Type> _consumerInterfacesHotSwap = new HotSwapList<Type>();

        /// <summary>
        /// Pending synchronous (and async result) calls.
        /// </summary>
        Dictionary<long, SyncCallInfo> _syncCalls = new Dictionary<long, SyncCallInfo>();

        /// <summary>
        /// The number of currently pending sync (or async result) calls.
        /// </summary>
        public int PendingSyncCallsCount
        {
            get { return _syncCalls.Count; }
        }

        volatile Matrix.Framework.SuperPool.Core.SuperPool _superPool;
        /// <summary>
        /// The instance of the super pool this client belongs to.
        /// </summary>
        public Matrix.Framework.SuperPool.Core.SuperPool SuperPool
        {
            get { return _superPool; }
        }

        volatile bool _autoControlInvoke = true;
        /// <summary>
        /// Should calls going to (Windows.Forms.Control) class child instances
        /// be automatically performed on the control UI thread (trough Invoke()).
        /// </summary>
        public bool AutoControlInvoke
        {
            get { return _autoControlInvoke; }
            set { _autoControlInvoke = value; }
        }

        TimeSpan _defaultSyncCallTimeout = TimeSpan.FromSeconds(3);
        /// <summary>
        /// The default timeout used with synchronous calls.
        /// </summary>
        public TimeSpan DefaultSyncCallTimeout
        {
            get { lock (_syncRoot) { return _defaultSyncCallTimeout; } }
            set { lock (_syncRoot) { _defaultSyncCallTimeout = value; } }
        }

        /// <summary>
        /// The default duplication mode used when sending envelopes.
        /// </summary>
        public Envelope.DuplicationModeEnum EnvelopeDuplicationMode { get; set; }

        /// <summary>
        /// Default duplication mode applied when sending to multiple receivers. 
        /// Default value is *Both* since it only gives a performance hit of approx. 10-15%.
        /// </summary>
        public Envelope.DuplicationModeEnum EnvelopeMultiReceiverDuplicationMode { get; set; }

        /// <summary>
        /// The instance of the object that consumes the incoming data.
        /// </summary>
        public override object Source
        {
            set
            {
                if (_superPool != null)
                {// TODO: clear this scenario.
                    //System.Diagnostics.Debug.Fail("Assigning a source after added to super pool, scenario may produce errors.");
                }

                if (base.Source == value)
                {
                    return;
                }

                object oldSource = base.Source;
                base.Source = value;

                if (value == null)
                {
                    _consumerInterfacesHotSwap.Clear();
                }
                else
                {
                    _consumerInterfacesHotSwap.SetToRange(
                        ReflectionHelper.GatherTypeAttributeMarkedInterfaces(value.GetType(), typeof(SuperPoolInterfaceAttribute)));
                }

                SuperPoolSourceUpdateDelegate delegateInstance = SourceUpdatedEvent;
                if (delegateInstance != null)
                {
                    delegateInstance(this, oldSource, value);
                }
            }
        }

        internal string Name
        {
            get
            {
                return Id.Name;
            }
        }

        public event SuperPoolSourceUpdateDelegate SourceUpdatedEvent;
        public event SuperPoolClientUpdateDelegate SuperPoolAssignedEvent;
        public event SuperPoolClientUpdateDelegate SuperPoolReleasedEvent;

        /// <summary>
        /// Constructor.
        /// </summary>
        public SuperPoolClient(ClientId id, object source)
            : base(id)
        {
            EnvelopeDuplicationMode = Envelope.DuplicationModeEnum.None;
            EnvelopeMultiReceiverDuplicationMode = Envelope.DuplicationModeEnum.DuplicateBoth;

            if (source != null)
            {
                this.Source = source;
            }

            _garbageCollectorTimer = new System.Threading.Timer(CollectGarbage, null, GarbageCollectorIntervalMs, GarbageCollectorIntervalMs);
        }

        /// <summary>
        /// Constructor, with specific source assgined.
        /// Source may be null for standalone usage.
        /// </summary>
        public SuperPoolClient(string name, object source)
            : this(new ClientId(name), source)
        { }


        public override void Dispose()
        {
            _garbageCollectorTimer.Dispose();

            lock (_syncCalls)
            {
                foreach (SyncCallInfo call in _syncCalls.Values)
                {
                    call.Dispose();
                }

                _syncCalls.Clear();
            }

            this.Source = null;
            SourceUpdatedEvent = null;

            base.Dispose();
        }

        void CollectGarbage(object state)
        {
            // Remove any timed out pending async calls.
            lock (_syncCalls)
            {
                List<long> removeCalls = null;

                foreach (SyncCallInfo call in _syncCalls.Values)
                {
                    if (call.IsMultiResponse && call.IsMultiResponseComplete)
                    {
                        if (removeCalls == null)
                        {
                            removeCalls = new List<long>();
                        }

                        removeCalls.Add(call.CallId);
                        break;
                    }
                }

                if (removeCalls != null)
                {
                    foreach (long id in removeCalls)
                    {
                        _syncCalls.Remove(id);
                    }
                }
            }
        }

        /// <summary>
        /// *Throws* Assign the contained instance of super pool, executed when adding client from pool.
        /// </summary>
        internal void AssignSuperPool(Matrix.Framework.SuperPool.Core.SuperPool superPool)
        {
            lock (_syncRoot)
            {
                if (_superPool != null && _superPool != superPool)
                {
                    throw new Exception("Client already assigned to another super pool.");
                }
                _superPool = superPool;
            }

            SuperPoolClientUpdateDelegate del = SuperPoolAssignedEvent;
            if (del != null)
            {
                del(this);
            }
        }

        /// <summary>
        /// Release the contained instance of super pool, executed when removing client from pool.
        /// </summary>
        internal void ReleaseSuperPool()
        {
            lock (_syncRoot)
            {
#if Matrix_Diagnostics
                SystemMonitor.ErrorIf(_superPool == null, "Super pool not assigned to client [" + this.ToString() + "] so no need to release.");
#endif
                _superPool = null;
            }

            SuperPoolClientUpdateDelegate del = SuperPoolReleasedEvent;
            if (del != null)
            {
                del(this);
            }
        }


        /// <summary>
        /// An envelope has arrived from the messaging infrastructure.
        /// </summary>
        protected override void OnPerformExecution(Envelope envelope)
        {
            object messageConsumer = Source;
            if (messageConsumer == null || envelope.Message.GetType() != typeof(SuperPoolCall))
            {// This is a not a super pool call, or no message consumer.
                base.OnPerformExecution(envelope);
                return;
            }

            try
            {
                SuperPoolCall call = envelope.Message as SuperPoolCall;

                if (call.State == SuperPoolCall.StateEnum.Responding)
                {// Response.
                    object response = call.Parameters.Length > 0 ? call.Parameters[0] : null;
                    Exception exception = call.Parameters.Length > 1 ? call.Parameters[1] as Exception : null;

                    long callId = call.Id;
                    
                    SyncCallInfo syncCallInfo = null;
                    lock (_syncCalls)
                    {
                        if (_syncCalls.TryGetValue(callId, out syncCallInfo))
                        {
                            if (syncCallInfo.IsMultiResponse == false)
                            {// Only remove single response ones, since we have 1 for sure.
                                _syncCalls.Remove(callId);
                            }
                        }
                        else
                        {
                            syncCallInfo = null;
                        }
                    }

                    if (syncCallInfo != null)
                    {
                        syncCallInfo.AcceptResponse(this, response, exception);

                        if (syncCallInfo.IsMultiResponse && syncCallInfo.IsMultiResponseComplete)
                        {
                            lock (_syncCalls)
                            {
                                _syncCalls.Remove(callId);
                            }
                        }
                    }
                }
                else if (call.State == SuperPoolCall.StateEnum.Requesting)
                {// Call (Request).
                    if (_consumerInterfacesHotSwap.Contains(call.MethodInfoLocal.ReflectedType))
                    {
                        object result = null;
                        Exception exception = null;
                        result = PerformCall(call, messageConsumer, out exception);

                        if (call.RequestResponse)
                        {
                            call.State = SuperPoolCall.StateEnum.Responding;
                            if (exception == null)
                            {
                                call.Parameters = new object[] { result };
                            }
                            else
                            {// Also transport the exception.
                                call.Parameters = new object[] { result, exception };
                            }

                            Matrix.Framework.SuperPool.Core.SuperPool pool = _superPool;
                            if (pool == null)
                            {
#if Matrix_Diagnostics
                                SystemMonitor.Error(this.GetType().Name + " has failed to find super pool instance, execution failed.");
#endif
                                return;
                            }

                            IMessageBus messageBus = pool.MessageBus;
                            if (messageBus == null)
                            {
#if Matrix_Diagnostics
                                SystemMonitor.Error(this.GetType().Name + " has failed to find super pool's message bus instance, execution failed.");
#endif
                                return;
                            }

                            messageBus.Respond(envelope, new Envelope(call) { DuplicationMode = EnvelopeDuplicationMode });
                        }
                        else
                        {
                            call.State = SuperPoolCall.StateEnum.Finished;
                        }
                    }
                    else
                    {
                        if (call.MethodInfoLocal == null)
                        {
#if Matrix_Diagnostics
                            SystemMonitor.OperationError(string.Format("Call with no method info assigned ignored."));
#endif
                        }
                        else
                        {
#if Matrix_Diagnostics
                            SystemMonitor.OperationError(string.Format("Call to [{0}] not recognized.", call.MethodInfoLocal.ToString()));
#endif
                        }
                    }
                }
                else if (call.State == SuperPoolCall.StateEnum.EventRaise)
                {
                    Exception exception;
                    object result = PerformCall(call, messageConsumer, out exception);
                    call.State = SuperPoolCall.StateEnum.Finished;
                }
            }
            catch (Exception ex)
            {// It is possible we encounter some invocation error (for ex. source type changed while call travelling)
                // so gracefully handle these here.
#if Matrix_Diagnostics
                SystemMonitor.OperationError("Execution failed", ex);
#endif
            }
        }

        protected object PerformCall(SuperPoolCall call, object target, out Exception exception)
        {
            object result = call.Call(target, AutoControlInvoke, out exception);
            if (exception != null)
            {
#if Matrix_Diagnostics
                SystemMonitor.OperationError(string.Format("Client [{0}] call [{1}] has caused an exception", this.Name, call.ToString()), exception);
#endif
            }

            return result;
        }

        /// <summary>
        /// A call has been made from us trough the proxy object.
        /// </summary>
        internal object ProcessCall(SuperPoolProxyCall proxyCall)
        {
            SuperPoolInvocation superPool = _superPool;
            if (superPool == null)
            {
#if Matrix_Diagnostics
                SystemMonitor.OperationError("Failed to find super pool (possible dispose).");
#endif
                return ProxyTypeManager.GetTypeDefaultValue(proxyCall.ReturnType);
            }

            IMessageBus messageBus = superPool.MessageBus;
            if (messageBus == null)
            {
#if Matrix_Diagnostics
                SystemMonitor.OperationError("Failed to find message bus (possible dispose).");
#endif
                return ProxyTypeManager.GetTypeDefaultValue(proxyCall.ReturnType);
            }

            if (proxyCall.Processed)
            {
#if Matrix_Diagnostics
                SystemMonitor.OperationError("Proxy call already processed.");
#endif
                return ProxyTypeManager.GetTypeDefaultValue(proxyCall.ReturnType);
            }

            if (proxyCall.Mode == SuperPoolProxyCall.ModeEnum.DirectCall)
            {
                MessageBusClient clientInstance = messageBus.GetLocalClientInstance(proxyCall.ReceiversIds[0]);
                if (clientInstance == null || clientInstance is SuperPoolClient == false)
                {
#if Matrix_Diagnostics
                    SystemMonitor.OperationError("Direct call failed, due to client not found or corresponding.");
#endif
                    return ProxyTypeManager.GetTypeDefaultValue(proxyCall.ReturnType);
                }
                else
                {// Perform the direct call.
                    // This is still fast, since caching is used.
                    FastInvokeHelper.FastInvokeHandlerDelegate delegateInstance = FastInvokeHelper.GetMethodInvoker(proxyCall.MethodInfo.ProxyMethodInfo, true, true);
                    return delegateInstance.Invoke(((SuperPoolClient)clientInstance).Source, proxyCall.Parameters);
                }
            }
            else if (proxyCall.Mode == SuperPoolProxyCall.ModeEnum.CallFirst)
            {
                ClientId firstId = this.Resolve(proxyCall.MethodInfo.ProxyMethodInfo.DeclaringType);
                if (firstId == null)
                {
#if Matrix_Diagnostics
                    SystemMonitor.OperationError("Call first failed, no client found for [" + proxyCall.MethodInfo.ProxyMethodInfo.DeclaringType.Name + "] interface.");
#endif

                    return ProxyTypeManager.GetTypeDefaultValue(proxyCall.ReturnType);
                }

                proxyCall.ReceiversIds = new List<ClientId>() { firstId };
            }

            SuperPoolCall call = new SuperPoolCall(superPool.GetUniqueCallId());

            call.Parameters = proxyCall.Parameters;
            call.MethodInfoLocal = proxyCall.MethodInfo.ProxyMethodInfo;

            call.State = SuperPoolCall.StateEnum.Requesting;
            //SuperPoolProxyCall.ModeEnum.Default ? SuperPoolCall.StateEnum.Requesting : SuperPoolCall.StateEnum.RequestingDirectCall;

            call.RequestResponse = proxyCall.IsSynchronous || proxyCall.IsAsyncResultExpecting;

            proxyCall.Processed = true;

            foreach (ParameterInfo info in call.MethodInfoLocal.GetParameters())
            {// Filter out ref, out and optional parameters.

                if (/*info.IsOptional ||*/ info.IsOut || info.IsRetval || info.IsOut || info.ParameterType.IsByRef)
                {
                    throw new NotImplementedException("Super pool calls do not support optional, out and ref parameters");
                }
            }

            // Prepare the synchronous structure (also handles waiting for the async results).
            SyncCallInfo syncCall = null;
            if (proxyCall.IsSynchronous || proxyCall.IsAsyncResultExpecting)
            {
                syncCall = new SyncCallInfo(call.Id) 
                               {
                                   AsyncResultState = proxyCall.AsyncResultState, 
                                   AsyncResultDelegate = proxyCall.AsyncResultDelegate,
                                   AsyncResultTimeout = proxyCall.AsyncResultTimeout,
                               };

                lock (_syncCalls)
                {
                    _syncCalls[call.Id] = syncCall;
                }
            }

            List<ClientId> receiversIndeces = null;
            if (proxyCall.ReceiversIds == null)
            {// No receiver indicates send to all, so that is what we do.
                if (proxyCall.MethodInfo == null || proxyCall.MethodInfo.ProxyOwnerType == null)
                {
#if Matrix_Diagnostics
                    SystemMonitor.Error("Failed to establish the required proxy call parameters.");
#endif
                    return ProxyTypeManager.GetTypeDefaultValue(proxyCall.ReturnType);
                }

                Type interfaceType = proxyCall.MethodInfo.GetBaseInterfaceType();
                if (interfaceType == null)
                {
#if Matrix_Diagnostics
                    SystemMonitor.Error("Failed to establish the base interface type.");
#endif
                    return ProxyTypeManager.GetTypeDefaultValue(proxyCall.ReturnType);
                }

                receiversIndeces = new List<ClientId>();
                foreach (ComponentId receiverId in superPool.GetInterfaceImplementors(interfaceType))
                {
                    if (receiverId != proxyCall.Sender.Id)
                    {
                        receiversIndeces.Add((ClientId)receiverId);
                    }

                    if (proxyCall.IsSynchronous && receiversIndeces.Count > 0)
                    {// Synchronous inadressed calls only execute agains a max of one provider.
                        break;
                    }
                }
            }
            else
            {
                receiversIndeces = proxyCall.ReceiversIds;
            }

            if (receiversIndeces.Count > 0)
            {
                if (syncCall != null && proxyCall.IsSynchronous)
                {// Prepare the event.
                    syncCall.Event = new ManualResetEvent(false);
                }

                Outcomes sendResult = messageBus.Send(this.Id, receiversIndeces, new Envelope(call) 
                    { DuplicationMode = EnvelopeDuplicationMode }, proxyCall.RequestConfirmTimeout, false);

                if (proxyCall.Outcome != null)
                {
                    proxyCall.Outcome.Result = sendResult;
                }

                if (sendResult != Outcomes.Success)
                {
#if Matrix_Diagnostics
                    SystemMonitor.OperationError(string.Format("Failed to send proxy call [{0}].", proxyCall.ToString()));
#endif
                    return ProxyTypeManager.GetTypeDefaultValue(proxyCall.ReturnType);
                }

                if (syncCall != null && proxyCall.IsSynchronous)
                {// Wait for response.
                    
                    if (syncCall.Event.WaitOne(proxyCall.Timeout.Value) == false)
                    {// Time out.
#if Matrix_Diagnostics
                        SystemMonitor.OperationWarning(string.Format("Proxy call timed out [{0}].", proxyCall.ToString()));
#endif
                        return ProxyTypeManager.GetTypeDefaultValue(proxyCall.ReturnType);
                    }
                    else
                    {// Waited and awaken.
                        return syncCall.Response;
                    }
                }
            }
            else
            {
#if Matrix_Diagnostics
                SystemMonitor.OperationWarning(string.Format("Failed to find invocation recipients for call [{0}].", proxyCall.ToString()));
#endif
            }

            return ProxyTypeManager.GetTypeDefaultValue(proxyCall.ReturnType);
        }

        /// <summary>
        /// Manually register this interface as part of the services the source
        /// provides. Once registered, incoming calls on this will be accepted.
        /// </summary>
        /// <param name="interfaceType"></param>
        /// <param name="verify">Should the verification be done against the interface.</param>
        /// <returns></returns>
        public bool RegisterConsumerInterface(Type interfaceType, bool verify)
        {
            if (verify)
            {
                object source = Source;
                if (source == null)
                {
                    return false;
                }

                List<Type> interfaces = new List<Type>(source.GetType().GetInterfaces());
                if (interfaces.Contains(interfaceType) == false)
                {
                    return false;
                }
            }

            return _consumerInterfacesHotSwap.AddUnique(interfaceType);
        }

        #region Calls Implementation

        /// <summary>
        /// Perform the actual call.
        /// </summary>
        protected TType DoCall<TType>(ComponentId receiverId, TimeSpan? requestConfirmTimeout, 
                                      TimeSpan? timeout, AsyncCallResultDelegate asyncResultDelegate, object asyncResultState,
                                      TimeSpan? asyncResultTimeout, SuperPoolProxyCall.ModeEnum callMode, CallOutcome outcome)
            where TType : class
        {
            Matrix.Framework.SuperPool.Core.SuperPool pool = _superPool;
            if (pool == null)
            {
                return null;
            }

            SuperPoolProxyCall proxyCall;
            TType result;

            if (pool.Call<TType>(this, receiverId, out result, out proxyCall) == false)
            {
                // Call failed.
                return null;
            }
            
            proxyCall.AsyncResultDelegate = asyncResultDelegate;
            proxyCall.AsyncResultState = asyncResultState;
            proxyCall.AsyncResultTimeout = asyncResultTimeout;

            proxyCall.RequestConfirmTimeout = requestConfirmTimeout;
            proxyCall.Timeout = timeout;

            proxyCall.Mode = callMode;
            proxyCall.Outcome = outcome;

            return result;
        }

        /// <summary>
        /// Perform the actual call.
        /// </summary>
        /// <param name="receivers">The list of recipients ids.</param>
        /// <param name="timeOut">The time out for sync calls, or null for async.</param>
        protected TType DoCallMany<TType>(IEnumerable<ComponentId> receivers, TimeSpan? timeOut)
            where TType : class
        {
            Matrix.Framework.SuperPool.Core.SuperPool pool = _superPool;
            if (pool == null)
            {
                return null;
            }

            SuperPoolProxyCall call;
            TType result;

            if (pool.Call<TType>(this, receivers, out result, out call))
            {
                call.Timeout = timeOut;
                return result;
            }

            // Call failed.
            return null;
        }

        #endregion

        #region Calls Public

        /// <summary>
        /// Synchronous call operation, with no specified receiver, 
        /// will try to find one (the first) provider and execute upon it.
        /// </summary>
        public TType CallSyncFirst<TType>(TimeSpan timeOut)
            where TType : class
        {
            return DoCall<TType>(null, null, timeOut, null, null, null, SuperPoolProxyCall.ModeEnum.Default, null);
        }

        /// <summary>
        /// Synchronous call operation.
        /// This will (try to) perform a synchronous call and wait for the result (if there is one).
        /// Default timeout is used.
        /// </summary>
        public TType CallSync<TType>(ComponentId receiverId)
            where TType : class
        {
            return DoCall<TType>(receiverId, null, DefaultSyncCallTimeout, null, null, null, SuperPoolProxyCall.ModeEnum.Default, null);
        }

        /// <summary>
        /// Synchronous call operation.
        /// This will (try to) perform a synchronous call and wait for the result (if there is one).
        /// </summary>
        public TType CallSync<TType>(ComponentId receiverId, TimeSpan timeOut)
            where TType : class
        {
            return DoCall<TType>(receiverId, null, timeOut, null, null, null, SuperPoolProxyCall.ModeEnum.Default, null);
        }

        /// <summary>
        /// A third option, to the Call and CallSync, 
        /// this method is in the middle of both.
        /// 
        /// Do a call, and wait for a response of the receiver 
        /// that it *actually received the call*. It will not wait or 
        /// return the result, nor will it wait for the result to be
        /// generated, only make sure the caller has received the call.
        /// </summary>
        /// <typeparam name="TType"></typeparam>
        /// <param name="receiverId"></param>
        /// <returns></returns>
        public TType CallConfirmed<TType>(ComponentId receiverId, TimeSpan? confirmTimeout, out CallOutcome outcome)
            where TType : class
        {
            outcome = new CallOutcome();
            return DoCall<TType>(receiverId, confirmTimeout, null, null, null, null, SuperPoolProxyCall.ModeEnum.Default, outcome);
        }

        /// <summary>
        /// Call and receive any result that may come with the usage of asyncDelegate; use state to 
        /// send any call identification information you may wish to send.
        /// </summary>
        public TType Call<TType>(ComponentId receiverId, AsyncCallResultDelegate asyncDelegate, object state)
            where TType : class
        {
            return DoCall<TType>(receiverId, null, null, asyncDelegate, state, null, SuperPoolProxyCall.ModeEnum.Default, null);
        }

        /// <summary>
        /// Call and receive any result that may come with the usage of asyncDelegate; use state to 
        /// track any call identification information you may wish to use in the callback.
        /// </summary>
        public TType Call<TType>(ComponentId receiverId, AsyncCallResultDelegate asyncDelegate) where TType : class
        {
            return DoCall<TType>(null, null, null, asyncDelegate, null, null, SuperPoolProxyCall.ModeEnum.Default, null);
        }

        /// <summary>
        /// Call and receive any result that may come with the usage of asyncDelegate; use state to 
        /// track any call identification information you may wish to use in the callback.
        /// </summary>
        /// <typeparam name="TType"></typeparam>
        /// <param name="asyncDelegate"></param>
        /// <param name="asyncResultTimeout"></param>
        /// <returns></returns>
        public TType CallAll<TType>(AsyncCallResultDelegate asyncDelegate, TimeSpan asyncResultTimeout) where TType : class
        {
            return DoCall<TType>(null, null, null, asyncDelegate, null, asyncResultTimeout, SuperPoolProxyCall.ModeEnum.Default, null);
        }

        /// <summary>
        /// Call and receive any result that may come with the usage of asyncDelegate; use state to 
        /// track any call identification information you may wish to use in the callback.
        /// 
        /// Since in this version no direct receiver is identified, make sure to specify a maximum number of
        /// results to accept, as well as how long to wait for results to come.
        /// </summary>
        /// <param name="maxResultCount">Maximum number of accepted results, as a result of this non addressed call.</param>
        /// <param name="resultWaitTimeout">How long to wait for results coming in.</param>
        public TType CallAll<TType>(AsyncCallResultDelegate asyncDelegate, object state, TimeSpan asyncResultTimeout)
            where TType : class
        {
            return DoCall<TType>(null, null, null, asyncDelegate, state, asyncResultTimeout, SuperPoolProxyCall.ModeEnum.Default, null);
        }

        /// <summary>
        /// Async call with no receiver, means call all available recipients 
        /// of this interface and method.
        /// </summary>
        public TType CallAll<TType>()
            where TType : class
        {
            return DoCall<TType>(null, null, null, null, null, null, SuperPoolProxyCall.ModeEnum.Default, null);
        }

        /// <summary>
        /// Async call to the first recipient found that implements this (TType) service.
        /// Typically local components are provided before remote ones.
        /// </summary>
        public TType CallFirst<TType>()
            where TType : class
        {
            return DoCall<TType>(null, null, null, null, null, null, SuperPoolProxyCall.ModeEnum.CallFirst, null);
        }

        /// <summary>
        /// Async call to a single receiver.
        /// </summary>
        public TType Call<TType>(ComponentId receiverId)
            where TType : class
        {
            return DoCall<TType>(receiverId, null, null, null, null, null, SuperPoolProxyCall.ModeEnum.Default, null);
        }

        /// <summary>
        /// Async call to multiple receivers. Execution may be concurrent.
        /// </summary>
        public TType Call<TType>(IEnumerable<ComponentId> receivers)
            where TType : class
        {
            return DoCallMany<TType>(receivers, null);
        }

        /// <summary>
        /// Advanced!!
        /// Perform a direct (fully synchronous) call, only applicable
        /// to receiver that is on the local super pool instance.
        /// 
        /// *Important* this is a direct call, and it will arrive and execute
        /// instantly - it may end up being executed before previously
        /// sent standard Calls(), since they
        /// 
        /// </summary>
        /// <typeparam name="TType"></typeparam>
        /// <param name="receiver"></param>
        /// <returns></returns>
        public TType CallDirectLocal<TType>(ComponentId receiver)
            where TType : class
        {
            return DoCall<TType>(receiver, null, null, null, null, null, SuperPoolProxyCall.ModeEnum.DirectCall, null);
        }

        #endregion

        #region Subscribe methods

        /// <summary>
        /// Subscribe to all events of this type.
        /// </summary>
        public TType SubscribeAll<TType>()
            where TType : class
        {
            return Subscribe<TType>(new EventSubscriptionRequest());
        }

        /// <summary>
        /// Subscribe to event on the given source.
        /// </summary>
        public TType Subscribe<TType>(ComponentId sourceId)
            where TType : class
        {
            return Subscribe<TType>(new EventSubscriptionRequest((ClientId)sourceId));
        }

        /// <summary>
        /// Perform an event subscription.
        /// All subscribes are expected to be asynchronous, 
        /// and executed agains the actual pool only.
        /// </summary>
        public TType Subscribe<TType>(EventSubscriptionRequest subscription)
            where TType : class
        {
            Matrix.Framework.SuperPool.Core.SuperPool pool = _superPool;
            if (pool == null)
            {
                return null;
            }

            TType result;
            if (pool.Subscribe<TType>(this, subscription, out result))
            {
                return result;
            }

            return null;
        }


        #endregion
    
        #region IMessageSuperPoolClient Members

        public ClientId Resolve<TInterfaceType>()
        {
            Matrix.Framework.SuperPool.Core.SuperPool pool = _superPool;
            if (pool != null)
            {
                foreach (ClientId id in pool.GetInterfaceImplementors(typeof(TInterfaceType)))
                {
                    return id;
                }
            }

            return null;
        }

        public List<ClientId>  ResolveAll<TInterfaceType>()
        {
            List<ClientId> result = new List<ClientId>();
            Matrix.Framework.SuperPool.Core.SuperPool pool = _superPool;
            if (pool != null)
            {
                result.AddRange(pool.GetInterfaceImplementors(typeof(TInterfaceType)));
            }

            return result;
        }

        public ClientId Resolve(Type interfaceType)
        {
            Matrix.Framework.SuperPool.Core.SuperPool pool = _superPool;
            if (pool != null)
            {
                foreach (ClientId id in pool.GetInterfaceImplementors(interfaceType))
                {
                    return id;
                }
            }

            return null;
        }

        public List<ClientId> ResolveAll(Type interfaceType)
        {
            List<ClientId> result = new List<ClientId>();
            Matrix.Framework.SuperPool.Core.SuperPool pool = _superPool;
            if (pool != null)
            {
                result.AddRange(pool.GetInterfaceImplementors(interfaceType));
            }

            return result;
        }

        #endregion
    }
}
