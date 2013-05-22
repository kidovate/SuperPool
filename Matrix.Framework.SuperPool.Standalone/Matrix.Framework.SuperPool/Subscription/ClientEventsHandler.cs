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
using Matrix.Framework.SuperPool.Clients;
using Matrix.Framework.SuperPool.Core;
using Matrix.Framework.SuperPool.DynamicProxy;
using Matrix.Common.Core.Collections;
using Matrix.Common.Core;

#if Matrix_Diagnostics
using Matrix.Common.Diagnostics;
#endif

namespace Matrix.Framework.SuperPool.Subscription
{
    /// <summary>
    /// Class manages subscribing to all possible client source events.
    /// It only serves as a way to manage calls incoming from those events,
    /// and does not actually store any subscription information, outside
    /// of that.
    /// </summary>
    internal class ClientEventsHandler : IDynamicProxyMethodSink, IDisposable
    {
        volatile SuperPoolSubscription _owner;

        volatile object _clientSource = null;
        
        volatile SuperPoolClient _client = null;
        /// <summary>
        /// The client we operate upon.
        /// </summary>
        public SuperPoolClient Client
        {
            get { return _client; }
        }
        
        /// <summary>
        /// MethodId vs EventSubscriptionInfo.
        /// </summary>
        HotSwapDictionary<int, EventHandlingInformation> _eventsMethods = new HotSwapDictionary<int, EventHandlingInformation>();

        /// <summary>
        /// Info stores information on handling a single event on the source of the client.
        /// It also contains the dynamic method that is appointed to (only) handling this event.
        /// </summary>
        public class EventHandlingInformation
        {
            public EventInfo EventInfo { get; set; }
            public Delegate DelegateInstance { get; set; }
            public GeneratedMethodInfo GeneratedMethodInfo { get; set; }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public ClientEventsHandler(SuperPoolSubscription owner, SuperPoolClient client)
        {
            _owner = owner;
            if (Initialize(client) == false)
            {
#if Matrix_Diagnostics
                SystemMonitor.Error("Failed to initialize subscription for client."); SystemMonitor.ErrorIf(Initialize(client) == false, "Failed to initialize subscription for client."); SystemMonitor.ErrorIf(Initialize(client) == false, "Failed to initialize subscription for client."); SystemMonitor.Error("Failed to initialize subscription for client.");
#endif
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void Dispose()
        {
            ReleaseCurrentClientSource();

            lock (this)
            {
                if (_client != null)
                {
                    _client.SourceUpdatedEvent -= new SuperPoolSourceUpdateDelegate(_client_SourceChangedEvent);
                    _client = null;
                }

                _owner = null;
            }
        }

        /// <summary>
        /// Assign and attach to the client.
        /// </summary>
        protected bool Initialize(SuperPoolClient client)
        {
            lock (this)
            {
                if (_client != null)
                {
                    return false;
                }

                _client = client;
                _client.SourceUpdatedEvent += new SuperPoolSourceUpdateDelegate(_client_SourceChangedEvent);
            }

            AssignClientSource(client.Source);
            return true;
        }

        void _client_SourceChangedEvent(ISuperPoolClient client, object oldSource, object newSource)
        {
            AssignClientSource(newSource);
        }

        protected void AssignClientSource(object source)
        {
            SuperPoolClient client = _client;
            if (client == null)
            {
#if Matrix_Diagnostics
                SystemMonitor.Error("Failed to add client source, since client not available (possible Dispose).");
#endif
                return;
            }

            SuperPoolSubscription owner = _owner;
            if (owner == null)
            {
#if Matrix_Diagnostics
                SystemMonitor.Error("Failed to add client source, since no owner is available (possible Dispose).");
#endif
                return;
            }

            ReleaseCurrentClientSource();

            _clientSource = source;

            if (_clientSource == null)
            {
#if Matrix_Diagnostics
                SystemMonitor.OperationWarning("Starting a client with no source attached.");
#endif
                return;
            }

            foreach (Type interfaceType in ReflectionHelper.GatherTypeAttributeMarkedInterfaces(source.GetType(), typeof(SuperPoolInterfaceAttribute)))
            {// Gather all events, from interfaces marked with [SuperPoolInterfaceAttribute].
                
                // Make sure to have created the corresponding proxy instance for this interface type.
                owner.ProxyTypeManager.ObtainInterfaceProxy(interfaceType);

                foreach (EventInfo eventInfo in interfaceType.GetEvents())
                {
                    Type delegateType = eventInfo.EventHandlerType;
                    GeneratedMethodInfo methodInfo = owner.ProxyTypeManager.Builder.GenerateDynamicMethodProxyDelegate(delegateType);

                    // Create delegate can operate in 2 modes:
                    // - create a static delegate like this (requires instnace upon call): info.Method.CreateDelegate(delegateType);
                    // - create an instance delegate like this (can be direct called): info.Method.CreateDelegate(delegateType, instance);

                    Delegate delegateInstance = methodInfo.StandaloneDynamicMethod.CreateDelegate(delegateType, this);
                    eventInfo.AddEventHandler(source, delegateInstance);

                    EventHandlingInformation subscriptionInfo = new EventHandlingInformation()
                                                                    {
                                                                        DelegateInstance = delegateInstance,
                                                                        EventInfo = eventInfo,
                                                                        GeneratedMethodInfo = methodInfo
                                                                    };

                    lock (this)
                    {
                        _eventsMethods.Add(methodInfo.Id, subscriptionInfo);
                    }
                }

            }
        }

        protected void ReleaseCurrentClientSource()
        {
            object currentSource = _clientSource;
            _clientSource = null;
            if (currentSource == null)
            {
                return;
            }

            lock (this)
            {
                // Release all current associations.
                foreach (KeyValuePair<int, EventHandlingInformation> pair in _eventsMethods)
                {
                    pair.Value.EventInfo.RemoveEventHandler(currentSource, pair.Value.DelegateInstance);
                }

                _eventsMethods.Clear();
            }
        }


        #region IDynamicProxyMethodSink Members

        /// <summary>
        /// An event was raised by our source.
        /// </summary>
        public void ReceiveDynamicMethodCall(int methodId, object[] parameters)
        {
            ReceiveDynamicMethodCallAndReturn(methodId, null, parameters);
            //EventSubscriptionInfo eventSubscriptionInfo;
            //if (_eventsMethods.TryGetValue(methodId, out eventSubscriptionInfo) == false)
            //{
            //    SystemMonitor.OperationError("Failed to find corresponding method info, invocation aborted (possible dispose).");
            //    return;
            //}

            //MessageSuperPoolSubscription owner = _owner;
            //if (owner == null)
            //{
            //    SystemMonitor.OperationError("Owner not assign, invocation aborted (possible dispose).");
            //    return;
            //}

            //owner.ProcessEventRaised(this, eventSubscriptionInfo, null, parameters);
        }

        /// <summary>
        /// An event (with return value) was raised by our source.
        /// </summary>
        public object ReceiveDynamicMethodCallAndReturn(int methodId, Type returnType, object[] parameters)
        {
            EventHandlingInformation eventSubscriptionInfo;
            if (_eventsMethods.TryGetValue(methodId, out eventSubscriptionInfo) == false)
            {
#if Matrix_Diagnostics
                SystemMonitor.OperationError("Failed to find corresponding method info, invocation aborted (possible dispose).");
#endif
                return ProxyTypeManager.GetTypeDefaultValue(returnType);
            }

            SuperPoolSubscription owner = _owner;
            if (owner == null)
            {
#if Matrix_Diagnostics
                SystemMonitor.OperationError("Owner not assign, invocation aborted (possible dispose).");
#endif
                return ProxyTypeManager.GetTypeDefaultValue(returnType);
            }

            return owner.ProcessEventRaised(this, eventSubscriptionInfo, returnType, parameters);
        }

        #endregion
    }
}
