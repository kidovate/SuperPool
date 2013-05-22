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
using System.Collections.ObjectModel;
using System.Reflection;
using System.Threading;
using Matrix.Common.Core;
using Matrix.Common.Core.Collections;
using Matrix.Common.Extended;
using Matrix.Framework.MessageBus.Core;
using Matrix.Framework.SuperPool.Call;
using Matrix.Framework.SuperPool.Clients;
using Matrix.Framework.SuperPool.DynamicProxy;
using Matrix.Framework.SuperPool.Subscription;

#if Matrix_Diagnostics
using Matrix.Common.Diagnostics.TracerCore.Items;
#endif

namespace Matrix.Framework.SuperPool.Core
{
    /// <summary>
    /// Extend super pool with subscriptions and event management.
    /// </summary>
    public abstract class SuperPoolSubscription : SuperPoolCallbacks, ISuperPoolIntercom
    {
        object _syncRoot = new object();

        /// <summary>
        /// Client id (vs) subscription information for this client.
        /// </summary>
        Dictionary<ClientId, ClientEventsHandler> _clients = new Dictionary<ClientId, ClientEventsHandler>();

        /// <summary>
        /// Exteded event name (vs) method subscription information.
        /// </summary>
        HotSwapDictionary<string, EventSubscriptionInfo> _eventSubscriptions = new HotSwapDictionary<string, EventSubscriptionInfo>();

        /// <summary>
        /// Constructor.
        /// </summary>
        public SuperPoolSubscription()
        {
        }

        /// <summary>
        /// Dispose.
        /// </summary>
        public override void Dispose()
        {
            List<ClientEventsHandler> clients;
            List<EventSubscriptionInfo> subscriptions;
            lock (_syncRoot)
            {
                subscriptions = GeneralHelper.EnumerableToList<EventSubscriptionInfo>(_eventSubscriptions.Values);

                // Clear methods first, thus removing clients is faster.
                _eventSubscriptions.Clear();

                // Take care to do an optimized hot swap teardown.
                clients = GeneralHelper.EnumerableToList(_clients.Values);
                
                _clients.Clear();
            }

            foreach (EventSubscriptionInfo methodSubscription in subscriptions)
            {
                methodSubscription.Dispose();
            }

            foreach (ClientEventsHandler clientInfo in clients)
            {
                clientInfo.Dispose();
            }

            base.Dispose();
        }

        protected override bool HandleClientAdded(IMessageBus messageBus, ClientId clientId)
        {
            // Make sure to have this done first, since it will send notifications of clients, and we
            // may need those for the establishment of events.
            if (base.HandleClientAdded(messageBus, clientId) == false || messageBus == null || clientId == null)
            {
                return false;
            }

            MessageBusClient clientInstance = messageBus.GetLocalClientInstance(clientId);

            // Will only work for local AND MessageSuperPoolClient typed clients.
            if (clientInstance is SuperPoolClient)
            {
                lock (_syncRoot)
                {
                    if (_clients.ContainsKey(clientInstance.Id))
                    {// Already added.
                        return false;
                    }

                    ClientEventsHandler subscription = new ClientEventsHandler(this, (SuperPoolClient)clientInstance);
                    _clients.Add(clientInstance.Id, subscription);
                }

            }
            else
            {
                List<string> sourceTypeNames = messageBus.GetClientSourceTypes(clientId);
                if (sourceTypeNames == null)
                {
#if Matrix_Diagnostics
                    InstanceMonitor.Error("Failed to obtain client [" + clientId.ToString() + "] source type.");
#endif
                    return false;
                }

                SuperPoolClient intercomClient = IntercomClient;
                if (intercomClient == null)
                {
#if Matrix_Diagnostics
                    InstanceMonitor.Error("Failed to obtain super pool main intercom client, so new client handling has failed.");
#endif
                    return false;
                }

                List<EventSubscriptionRequest> totalRequests = new List<EventSubscriptionRequest>();
                if (clientId.IsLocalClientId == false)
                {
                    // Gather all the Super Pool related interfaces and their events, and send global updates for those
                    // so that any pending subscriptions may be restored.
                    // This is only done where the client is a remote client instance, since local ones we already know 
                    // of them. This eventing information must be at the local pool for the client, since it is the one
                    // handling the event and sending it to all interested parties.
                    foreach (Type interfaceType in ReflectionHelper.GetKnownTypes(sourceTypeNames))
                    {
                        if (interfaceType.IsInterface 
                            && ReflectionHelper.TypeHasCustomAttribute(interfaceType, typeof(SuperPoolInterfaceAttribute), false) == false)
                        {// Interface type not marked as super pool.
                            continue;
                        }

                        foreach (EventInfo info in interfaceType.GetEvents())
                        {
                            string eventName = GeneralHelper.GetEventMethodExtendedName(info, false);
                            EventSubscriptionInfo eventInfo;
                            if (_eventSubscriptions.TryGetValue(eventName, out eventInfo))
                            {
                                totalRequests.AddRange(eventInfo.GatherSourceRelatedUpdates(clientId));
                            }
                        }
                    }
                }

                // Send updates for the newly connected client, so that it can obtain any subscription information
                // regarding it, it case it has missed some.
                foreach (EventSubscriptionRequest request in totalRequests)
                {
                    // Notify other connected super pools of this subcription, 
                    // since the subscribee(s) may be attached on them.
                    // *pendingCall swap done here, make sure to not use it on or after this line*
                    intercomClient.CallAll<ISuperPoolIntercom>().ProcessSubscriptionUpdate(request);
                }
            }


            return true;
        }

        /// <summary>
        /// Remove all subscription references for this client.
        /// </summary>
        /// <returns></returns>
        protected override bool HandleClientRemoved(IMessageBus messageBus, ClientId clientId, bool isPermanent)
        {
            if (base.HandleClientRemoved(messageBus, clientId, isPermanent) == false)
            {
                return false;
            }

            if (isPermanent)
            {// Only cleanup subscriptions in case remove was permanent.
                foreach (EventSubscriptionInfo subscription in _eventSubscriptions.Values)
                {
                    subscription.RemoveClientSubscriptions(clientId);
                }
            }

            ClientEventsHandler clientInfo = null;
            lock (_syncRoot)
            {
                if (_clients.TryGetValue(clientId, out clientInfo) == false)
                {// Client not added, possibly not a local client.
                    return true;
                }

                if (_clients.Remove(clientId) == false)
                {
#if Matrix_Diagnostics
                    InstanceMonitor.OperationError("Failed to remove client from subscription lists.");
#endif
                }
            }

            if (clientInfo != null)
            {
                clientInfo.Dispose();
            }

            return true;
        }

        #region Public Invocation

        #endregion

        /// <summary>
        /// Perform event subscription (Subscribe), always asynchronous.
        /// </summary>
        public bool Subscribe<TType>(SuperPoolClient subscriber, 
                                     EventSubscriptionRequest request, out TType resultValue)
            where TType : class
        {
            SuperPoolProxyCall call;
            bool result = Call<TType>(subscriber, out resultValue, out call);
            call.SubscriptionRequest = request;

            return result;
        }

        /// <summary>
        /// Handle event subscription (Proxy.Event.Subscribe)
        /// </summary>
        protected override void ProcessReceiveEventSubscription(int methodId, Delegate delegateInstance, bool isAdd)
        {
            SuperPoolProxyCall pendingCall = null;
            if (_pendingThreadsCalls.TryGetValue(Thread.CurrentThread.ManagedThreadId, out pendingCall) == false)
            {
#if Matrix_Diagnostics
                InstanceMonitor.OperationError("Failed to find corresponding thread proxy call information.");
#endif
                return;
            }

            EventSubscriptionRequest subscriptionRequest = pendingCall.SubscriptionRequest;
            if (subscriptionRequest == null)
            {
#if Matrix_Diagnostics
                InstanceMonitor.OperationError("Failed to find corresponding subscription requests, event subscription failed.");
#endif
                return;
            }

            if (pendingCall.Sender == null || pendingCall.Sender.Id == null)
            {
#if Matrix_Diagnostics
                InstanceMonitor.OperationError("Failed to establish subscription sender information, subscription failed.");
#endif
                return;
            }

            if (delegateInstance.Target != pendingCall.Sender.Source)
            {
#if Matrix_Diagnostics
                InstanceMonitor.Error("Only a message super pool client source can subscribe to events.");
#endif
                return;
            }

            ProxyTypeBuilder builder = ProxyTypeBuilder;
            if (builder == null)
            {
#if Matrix_Diagnostics
                InstanceMonitor.OperationError("Failed to find proxy type builder, event subscription failed.");
#endif
                return;
            }

            GeneratedMethodInfo generatedMethodInfo = builder.GetMethodInfoById(methodId);
            if (generatedMethodInfo == null)
            {
#if Matrix_Diagnostics
                InstanceMonitor.OperationError("Failed to find method [id, " + methodId + "] info, event subscription failed.");
#endif
                return;
            }

            if (string.IsNullOrEmpty(generatedMethodInfo.EventName))
            {
                generatedMethodInfo.EventName = GeneralHelper.GetEventExtendedNameByMethod(generatedMethodInfo.GetMethodInfo(), false, true);
            }

            // generatedMethodInfo.GetMethodInfo() >> I2.add_AEVent
            string extendedEventName = generatedMethodInfo.EventName;
            MethodInfo eventAddMethodInfo = generatedMethodInfo.GetMethodInfo();

            // *IMPORTANT* the Call<> will cause the currently used pendingCall to be repopulated with information,
            // so we ned to extract the *sender id* BEFORE calling the actual Call(), since it will change the
            // pendingCall instance immediately.
            subscriptionRequest.SenderId = pendingCall.Sender.Id;
            subscriptionRequest.ExtendedEventName = extendedEventName;
            subscriptionRequest.IsAdd = isAdd;
            //subscriptionRequest.EventAddMethodInfo = eventAddMethodInfo;
            subscriptionRequest.DelegateInstanceMethodInfo = delegateInstance.Method;

            // Process locally.
            ((ISuperPoolIntercom)this).ProcessSubscriptionUpdate(subscriptionRequest);

            SuperPoolClient mainClient = IntercomClient;
            if (mainClient == null)
            {
#if Matrix_Diagnostics
                InstanceMonitor.Error("Failed to obtain super pool main intercom client, so new client handling has failed.");
#endif
            }
            else
            {
                // Notify other connected super pools of this subcription, 
                // since the subscribee(s) may be attached on them.
                // *pendingCall swap done here, make sure to not use it on or after this line*
                mainClient.CallAll<ISuperPoolIntercom>().ProcessSubscriptionUpdate(subscriptionRequest);
            }

        }

        void ISuperPoolIntercom.ProcessSubscriptionUpdate(EventSubscriptionRequest subscriptionRequest)
        {
            // Filter request, since it may not be related at all to this super pool.
            bool clientFound = false;
            ReadOnlyCollection<ClientId> sources = subscriptionRequest.EventsSources;
            if (sources == null)
            {// Null value indicates a subscirption to all possible sources.
                clientFound = true;
            }
            else
            {
                foreach (ClientId id in sources)
                {
                    if (_clients.ContainsKey(id))
                    {
                        clientFound = true;
                        break;
                    }
                }
            }

            // Check the sources and the SenderId, before dumping event.
            if (clientFound == false && subscriptionRequest.SenderId.IsLocalClientId == false)
            {
#if Matrix_Diagnostics
                InstanceMonitor.Info("Subscription request received [" + subscriptionRequest.ToString() + "], ignored since not related.");
#endif
                return;
            }
            else
            {
#if Matrix_Diagnostics
                InstanceMonitor.Info("Subscription request received [" + subscriptionRequest.ToString() + "] and processing...");
#endif
            }

            EventSubscriptionInfo methodSubscription = null;
            if (_eventSubscriptions.TryGetValue(subscriptionRequest.ExtendedEventName, out methodSubscription) == false)
            {
                lock (_syncRoot)
                {
                    if (_eventSubscriptions.TryGetValue(subscriptionRequest.ExtendedEventName, out methodSubscription) == false)
                    {// Add a new method subscription info.
                        methodSubscription = new EventSubscriptionInfo(subscriptionRequest.ExtendedEventName);
                        _eventSubscriptions.Add(subscriptionRequest.ExtendedEventName, methodSubscription);
                    }
                }
            }

            if (methodSubscription == null)
            {
#if Matrix_Diagnostics
                InstanceMonitor.OperationError("Failed to find method subscription, subscription failed.");
#endif
                return;
            }

            // Apply the requests locally.
            methodSubscription.SubscriptionUpdate(subscriptionRequest);
        }

        /// <summary>
        /// Client source has raised an event, process it.
        /// </summary>
        internal object ProcessEventRaised(ClientEventsHandler client,
                                           ClientEventsHandler.EventHandlingInformation eventSubscriptionInfo, Type returnType, object[] parameters)
        {
            if (string.IsNullOrEmpty(eventSubscriptionInfo.GeneratedMethodInfo.EventName))
            {
                // Establish the name of the event.
                eventSubscriptionInfo.GeneratedMethodInfo.EventName = 
                    GeneralHelper.GetEventMethodExtendedName(eventSubscriptionInfo.EventInfo, false);
            }

            EventSubscriptionInfo eventSubscription = null;

            if (string.IsNullOrEmpty(eventSubscriptionInfo.GeneratedMethodInfo.EventName) == false && 
                _eventSubscriptions.TryGetValue(eventSubscriptionInfo.GeneratedMethodInfo.EventName, out eventSubscription))
            {// OK, to perform the calls.

                // Process specific subscribers
                foreach (KeyValuePair<ClientId, EventSubscriptionInfo.ClientEventSubscriptionInfo> pair 
                    in eventSubscription.GetReceivers(client.Client.Id, true))
                {
                    foreach(KeyValuePair<MethodInfo, int> subPair in pair.Value.Data)
                    {
                        for (int i = 0; i < subPair.Value; i++)
                        {// May need to raise multiple times.
                            if (client.Client.Id != pair.Key)
                            {// Filter out subscriptions by the one that raised it.
                                ProcessEventCall(client.Client.Id, pair.Key, subPair.Key, parameters);
                            }
                        }
                    }
                }

                // Process subscribe to all.
                foreach (KeyValuePair<ClientId, EventSubscriptionInfo.ClientEventSubscriptionInfo> pair
                    in eventSubscription.GetReceivers(client.Client.Id, false))
                {
                    foreach (KeyValuePair<MethodInfo, int> subPair in pair.Value.Data)
                    {
                        for (int i = 0; i < subPair.Value; i++)
                        {// May need to raise multiple times.
                            if (client.Client.Id != pair.Key)
                            {// Filter out subscriptions by the one that raised it.
                                ProcessEventCall(client.Client.Id, pair.Key, subPair.Key, parameters);
                            }
                        }
                    }
                }

            }
            else
            {// No subscription(s) for this event.
#if Matrix_Diagnostics
                InstanceMonitor.Info(string.Format("Event raised [{0}] had no subscribers.", eventSubscriptionInfo.GeneratedMethodInfo.EventName), TracerItem.PriorityEnum.Trivial);
#endif
            }
            
            // Return a default value.
            return ProxyTypeManager.GetTypeDefaultValue(returnType);
        }

        /// <summary>
        /// Process an event call.
        /// </summary>
        void ProcessEventCall(ClientId senderId, ClientId receiverId, 
                              MethodInfo targetMethodInfo, object[] parameters)
        {
            if (receiverId == null)
            {
#if Matrix_Diagnostics
                InstanceMonitor.Error("Proxy call receiver not received.");
#endif
                return;
            }

            IMessageBus messageBus = MessageBus;
            if (messageBus == null)
            {
#if Matrix_Diagnostics
                InstanceMonitor.OperationError("Failed to find message bus (possible dispose).");
#endif
                return;
            }

            SuperPoolCall call = new SuperPoolCall(GetUniqueCallId());

            call.Parameters = parameters;
            call.MethodInfoLocal = targetMethodInfo;

            call.State = SuperPoolCall.StateEnum.EventRaise;

            if (messageBus.Send(senderId, receiverId,
                                new Envelope(call) { DuplicationMode = Envelope.DuplicationModeEnum.None }, null, false) != Outcomes.Success)
            {
#if Matrix_Diagnostics
                InstanceMonitor.OperationError("Failed to send event proxy call.");
#endif
            }
        }


    }
}
