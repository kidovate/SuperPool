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
using Matrix.Framework.MessageBus.Core;
using Matrix.Framework.SuperPool.Clients;
using Matrix.Framework.SuperPool.DynamicProxy;
using Matrix.Common.Core.Collections;
using Matrix.Common.Core;
#if Matrix_Diagnostics
using Matrix.Common.Diagnostics;

#endif

namespace Matrix.Framework.SuperPool.Core
{
    /// <summary>
    /// Top of the message super pool class stack, this is the one that manages clients.
    /// </summary>
    public class SuperPoolClients : IDisposable
    {
        protected volatile ProxyTypeManager _proxyTypeManager = null;

        /// <summary>
        /// Manages proxy objects creation and operation.
        /// </summary>
        internal ProxyTypeManager ProxyTypeManager
        {
            get { return _proxyTypeManager; }
        }

        /// <summary>
        /// A collection indicating all the types and what clients implement them.
        /// 
        /// *IMPORTANT* hot swap specific functionality used, do not refactor without consideration.
        /// </summary>
        protected HotSwapDictionary<Type, HotSwapList<ClientId>> _clientsInterfaces = new HotSwapDictionary<Type, HotSwapList<ClientId>>();

        protected IMessageBus _messageBus;
        /// <summary>
        /// The message bus the super pool uses for communication.
        /// </summary>
        public IMessageBus MessageBus
        {
            get { return _messageBus; }
        }

        /// <summary>
        /// Client of the super pool, used to all separate pool instances to talk to each other.
        /// </summary>
        protected SuperPoolClient IntercomClient { get; private set; }


#if Matrix_Diagnostics
        /// <summary>
        /// 
        /// </summary>
        protected InstanceMonitor InstanceMonitor { get; private set; }
#endif

        /// <summary>
        /// Name of the super pool, same as the name of the underlying bus.
        /// </summary>
        public string Name
        {
            get
            {
                IMessageBus messageBus = _messageBus;
                if (messageBus == null)
                {
                    return string.Empty;
                }

                return messageBus.Name;
            }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public SuperPoolClients()
        {
#if Matrix_Diagnostics
            InstanceMonitor = new InstanceMonitor(this);
#endif
            _proxyTypeManager = new ProxyTypeManager();
        }

        public virtual void Dispose()
        {
            ProxyTypeManager manager = _proxyTypeManager;
            if (manager != null)
            {
                manager.Dispose();
                _proxyTypeManager = null;
            }

            IMessageBus messageBus = _messageBus;
            _messageBus = null;

            if (messageBus != null)
            {
                messageBus.ClientAddedEvent -= new MessageBusClientUpdateDelegate(_messageBus_ClientAddedEvent);
                messageBus.ClientRemovedEvent -= new MessageBusClientRemovedDelegate(_messageBus_ClientRemovedEvent);
                messageBus.ClientUpdateEvent -= new MessageBusClientUpdateDelegate(_messageBus_ClientUpdateEvent);

                messageBus.Dispose();
            }
        }

        /// <summary>
        /// Initialize the pool for operation, by supplying it with a message bus.
        /// </summary>
        protected virtual bool Initialize(IMessageBus messageBus)
        {
            lock (this)
            {
                if (_messageBus != null || messageBus == null)
                {
                    return false;
                }

                _messageBus = messageBus;
                _messageBus.ClientAddedEvent += new MessageBusClientUpdateDelegate(_messageBus_ClientAddedEvent);
                _messageBus.ClientRemovedEvent += new MessageBusClientRemovedDelegate(_messageBus_ClientRemovedEvent);
                _messageBus.ClientUpdateEvent += new MessageBusClientUpdateDelegate(_messageBus_ClientUpdateEvent);

                // Add a client with self to the message bus.
                IntercomClient = new SuperPoolClient("SuperPool.Intercom", this);
            }

            if (this.AddClient(IntercomClient) == false)
            {
#if Matrix_Diagnostics
                InstanceMonitor.Fatal("Failed to add super pool main client.");
#endif
                lock (this)
                {
                    IntercomClient.Dispose();
                    IntercomClient = null;

                    _messageBus.ClientAddedEvent -= new MessageBusClientUpdateDelegate(_messageBus_ClientAddedEvent);
                    _messageBus.ClientRemovedEvent -= new MessageBusClientRemovedDelegate(_messageBus_ClientRemovedEvent);
                    _messageBus.ClientUpdateEvent -= new MessageBusClientUpdateDelegate(_messageBus_ClientUpdateEvent);
                    _messageBus = null;
                }

                return false;
            }

            return true;
        }

        protected virtual bool HandleClientAdded(IMessageBus messageBus, ClientId clientId)
        {
            Type clientType = messageBus.GetClientType(clientId);
            if (clientType == null)
            {
#if Matrix_Diagnostics
                InstanceMonitor.OperationError("Failed to establish client type.");
#endif
                return false;
            }

            if (clientType != typeof(SuperPoolClient) && 
                clientType.IsSubclassOf(typeof(SuperPoolClient)) == false)
            {// Client not a super pool client.
                return false;
            }

            RegisterClientSourceTypes(clientId);

            return true;
        }

        protected virtual bool HandleClientRemoved(IMessageBus messageBus, ClientId clientId, bool isPermanent)
        {
            Type clientType = messageBus.GetClientType(clientId);
            if (clientType == null)
            {
#if Matrix_Diagnostics
                InstanceMonitor.OperationError("Failed to establish client type.");
#endif
                return false;
            }

            if (clientType != typeof(SuperPoolClient) &&
                clientType.IsSubclassOf(typeof(SuperPoolClient)) == false)
            {// Client not a super pool client.
                return false;
            }

            UnRegisterClientSourceTypes(clientId);

            return true;
        }

        void _messageBus_ClientAddedEvent(IMessageBus messageBus, ClientId clientId)
        {
            HandleClientAdded(messageBus, clientId);
        }

        protected virtual void _messageBus_ClientRemovedEvent(IMessageBus messageBus, ClientId clientId, bool isPermanent)
        {
            HandleClientRemoved(messageBus, clientId, isPermanent);
        }

        void _messageBus_ClientUpdateEvent(IMessageBus messageBus, ClientId clientId)
        {
            UnRegisterClientSourceTypes(clientId);
            RegisterClientSourceTypes(clientId);
        }

        /// <summary>
        /// Obtain a collection of the Ids of all clients that implement the interface.
        /// </summary>
        /// <param name="interfaceType"></param>
        /// <returns>The actual hot swap instance, of the collection with the interfaces, thus making it an ultra-fast (instant) result.</returns>
        public ClientId GetFirstInterfaceImplementor(Type interfaceType)
        {
            HotSwapList<ClientId> result = null;
            if (_clientsInterfaces.TryGetValue(interfaceType, out result) == false)
            {
                return null;
            }

            if (result.Count > 0)
            {
                return result[0];
            }

            return null;
        }

        /// <summary>
        /// Obtain a collection of the Ids of all clients that implement the interface.
        /// </summary>
        /// <param name="interfaceType"></param>
        /// <returns>The actual hot swap instance, of the collection with the interfaces, thus making it an ultra-fast (instant) result.</returns>
        public IEnumerable<ClientId> GetInterfaceImplementors(Type interfaceType)
        {
            HotSwapList<ClientId> result = null;
            if (_clientsInterfaces.TryGetValue(interfaceType, out result) == false)
            {
                return new ClientId[] { };
            }

            return result;
        }

        /// <summary>
        /// Add a client to the pool.
        /// </summary>
        public virtual bool AddClient(SuperPoolClient client)
        {
            IMessageBus messageBus = _messageBus;
            if (messageBus == null)
            {
                return false;
            }

            if (client.Source == null)
            {// TODO: clear this scenario.
                //System.Diagnostics.Debug.Fail("Warning, adding a client with no source assigned. Make sure to assign source prior to adding client.");
            }

            bool result = messageBus.AddClient(client);
            if (result)
            {
                client.AssignSuperPool((SuperPool)this);
            }

            return result;
        }

        /// <summary>
        /// Remove a client from the pool.
        /// </summary>
        public virtual bool RemoveClient(SuperPoolClient client, bool isPermanent)
        {
            IMessageBus messageBus = _messageBus;
            if (messageBus == null)
            {
                return false;
            }

            bool result = messageBus.RemoveClient(client, isPermanent);
            if (result)
            {
                client.ReleaseSuperPool();
            }

            return result;
        }


        /// <summary>
        /// 
        /// </summary>
        bool RegisterClientSourceTypes(ClientId clientId)
        {
            IMessageBus messageBus = _messageBus;
            if (messageBus == null)
            {
                Console.WriteLine("Failed to register client source type, message bus not found.");
#if Matrix_Diagnostics
                InstanceMonitor.OperationError("Failed to register client source type, message bus not found.");
#endif
                return false;
            }

            List<string> sourceTypes = messageBus.GetClientSourceTypes(clientId);
            if (sourceTypes == null)
            {
                Console.WriteLine("Failed to register client source type, source type not found.");
#if Matrix_Diagnostics
                InstanceMonitor.OperationError("Failed to register client source type, source type not found.");
#endif
                return false;
            }

            foreach (Type superType in ReflectionHelper.GetKnownTypes(sourceTypes))
            {
                if (superType.IsInterface == false || 
                    ReflectionHelper.TypeHasCustomAttribute(superType, typeof(SuperPoolInterfaceAttribute), false) == false)
                {
                    continue;
                }

                HotSwapList<ClientId> clientList = null;
                if (_clientsInterfaces.TryGetValue(superType, out clientList) == false)
                {
                    clientList = _clientsInterfaces.GetOrAdd(superType, new HotSwapList<ClientId>());
                }

                clientList.AddUnique(clientId);

                if (ReflectionHelper.TypeHasCustomAttribute(superType, typeof(SuperPoolInterfaceAttribute), false) == false)
                {// Register this type as well.
                    _proxyTypeManager.ObtainInterfaceProxy(superType);
                }
            }

            return true;
        }

        /// <summary>
        /// Will unregister all _clientInterfaces associations with this client.
        /// </summary>
        /// <param name="clientId"></param>
        void UnRegisterClientSourceTypes(ClientId clientId)
        {
            foreach (KeyValuePair<Type, HotSwapList<ClientId>> pair in _clientsInterfaces)
            {
                pair.Value.Remove(clientId);
            }
        }

    }
}
