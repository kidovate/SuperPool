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
using System.Threading;
using Matrix.Common.Core;
using Matrix.Common.Core.Collections;
using Matrix.Common.Core.Serialization;
using Matrix.Framework.MessageBus.Clients.ExecutionStrategies;

#if Matrix_Diagnostics
    using Matrix.Common.Diagnostics;
#endif

namespace Matrix.Framework.MessageBus.Core
{
    /// <summary>
    /// A second generation implementation of the communication framework (first was based on "Arbiter" system), this one designed with speed in mind.
    /// The model allows to execution of "items" (or messages) on one or multiple close or distant "clients".
    /// </summary>
    public class MessageBus : MessageBusBase
    {
        protected object _syncRoot = new object();

        long _pendingStampId = 0;
        protected long PendingStampId
        {
            get
            {
                return Interlocked.Increment(ref _pendingStampId);
            }
        }

        /// <summary>
        /// Hot swapping - this is the fastes way of all to access a client, 
        /// without holding an actual reference, and no locks either.
        /// The client Id must contain the index of the list.
        /// This index will never change, since we shall only add items to the list.
        /// 
        /// To evade locking - when adding new items, simply replace the list with a new one.
        /// *WARNING* Items are never removed from list, only set to NULL.
        /// </summary>
        HotSwapList<MessageBusClient> _clientsHotSwap = new HotSwapList<MessageBusClient>();
        
        protected ReadOnlyCollection<MessageBusClient> Clients
        {
            get { return _clientsHotSwap.AsReadOnly(); }
        }

        readonly ISerializer _serializer = new BinarySerializer();
        /// <summary>
        /// The serializer the bus uses when transproting messages. 
        /// By default, JSON.Net is used.
        /// This must be assigned at startup, since child constructors may use it.
        /// </summary>
        public ISerializer Serializer
        {
            get { return _serializer; }
        }

        HotSwapDictionary<Guid, int> _guidToIndexHotSwap = new HotSwapDictionary<Guid, int>();

        /// <summary>
        /// Constructor.
        /// </summary>
        public MessageBus(string name)
            : base(name)
        {
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public MessageBus(string name, ISerializer serializer)
            : base(name)
        {
            _serializer = serializer;
        }

        /// <summary>
        /// Will return negative value (for ex. -1, or see InvalidClientIndex) to indicate not found.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        protected int GetClientIndexByGuid(Guid guid)
        {
            int result = 0;
            if (_guidToIndexHotSwap.TryGetValue(guid, out result) == false)
            {
                return ClientId.InvalidMessageBusClientIndex;
            }

            return result;
        }

        /// <summary>
        /// Helper, works on local clients only.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        protected ClientId GetLocalClientIdByIndex(int id)
        {
            MessageBusClient client = GetLocalClientByIndex(id);

            if (client == null)
            {
                return null;
            }

            return client.Id;
        }

        /// <summary>
        /// Obtain client based on its index.
        /// </summary>
        protected MessageBusClient GetLocalClientByIndex(int id)
        {
            MessageBusClient result = null;
            if (_clientsHotSwap.TryGetValue(id, ref result))
            {
                return result;
            }

            return null;
        }

        public override List<ClientId> GetAllClientsIds()
        {
            List<ClientId> result = new List<ClientId>();
            foreach (MessageBusClient client in this._clientsHotSwap)
            {
                if (client == null)
                {// This is normal since items are never removed from list, only set to null.
                    continue;
                }

                ClientId id = client.Id;
                if (id != null)
                {
                    result.Add(id);
                }
            }

            return result;
        }


        public override MessageBusClient GetLocalClientInstance(ClientId clientId)
        {
            if (clientId.MessageBus == this && clientId.IsMessageBusIndexValid)
            {
                return _clientsHotSwap[clientId.LocalMessageBusIndex];
            }

            return null;
        }

        /// <summary>
        /// Obtain the type of the client with this id, if client is available.
        /// </summary>
        public override Type GetClientType(ClientId clientId)
        {
            if (clientId.MessageBus != this)
            {
                return null;
            }

            MessageBusClient client = GetLocalClientByIndex(clientId.LocalMessageBusIndex);
            if (client == null)
            {
                return null;
            }

            return client.GetType();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="clientId"></param>
        /// <returns></returns>
        public override List<string> GetClientSourceTypes(ClientId clientId)
        {
            if (clientId.MessageBus != this)
            {
                return null;
            }

            MessageBusClient client = GetLocalClientByIndex(clientId.LocalMessageBusIndex);
            if (client == null)
            {
                return null;
            }

            return ReflectionHelper.GetTypeNameAndRelatedTypes(client.OptionalSourceType);
        }


        /// <summary>
        /// 
        /// </summary>
        public override bool ContainsClient(ClientId clientId)
        {
            return _guidToIndexHotSwap.ContainsKey(clientId.Guid);
        }

        /// <summary>
        /// Needs to consume both client and id, since it may be used with client == null.
        /// </summary>
        /// <returns>The index of the newly registered client, or InvalidClientIndex on failure.</returns>
        protected int DoAddClient(MessageBusClient client, ClientId clientId)
        {
            int index = 0;

            if (client != null && client.Id.Equals(clientId) == false)
            {
#if Matrix_Diagnostics
                InstanceMonitor.Error("Client id mismatch.");
#endif
                return ClientId.InvalidMessageBusClientIndex;
            }

            lock (_syncRoot)
            {
                if (clientId.LocalMessageBusIndex != ClientId.InvalidMessageBusClientIndex)
                {// Client already has an Index assigned, must reuse it.

                    MessageBusClient existingInstance = null;
                    if (_clientsHotSwap.TryGetValue(clientId.LocalMessageBusIndex, ref existingInstance))
                    {// Successfully acquired existing value for client.

                        // Check if we are OK to assign to this position.
                        if (existingInstance != null && existingInstance != client)
                        {// There is something else at that position.
#if Matrix_Diagnostics
                            InstanceMonitor.Error("Client id mismatch.");
#endif
                            return ClientId.InvalidMessageBusClientIndex;
                        }

                    }
                    else
                    {// Failed to acquire value with this message bus index.
#if Matrix_Diagnostics
                        InstanceMonitor.Error("Client with this message bus index can not be assigned.");
#endif
                        return ClientId.InvalidMessageBusClientIndex;
                    }

                    // Assign the client to its former spot.
                    _clientsHotSwap[clientId.LocalMessageBusIndex] = client;
                    index = clientId.LocalMessageBusIndex;
                }
                else
                {
                    if (GetClientIndexByGuid(clientId.Guid) >= 0)
                    {// Already added.
#if Matrix_Diagnostics
                        InstanceMonitor.Error("Message bus client [" + clientId.ToString() + "] added more than once.");
#endif
                        return ClientId.InvalidMessageBusClientIndex;
                    }

                    // Add the client to a new spot.
                    _clientsHotSwap.Add(client);
                    index = _clientsHotSwap.Count - 1;
                }


                // This type of assignment will also work with multiple entries.
                // This performs an internal hotswap.
                _guidToIndexHotSwap[clientId.Guid] = index;
            }

            if (client != null &&
                client.AssignMessageBus(this, index) == false)
            {
#if Matrix_Diagnostics
                InstanceMonitor.OperationError("A client has denied adding to Message bus.");
#endif
                RemoveClient(client, true);
                return ClientId.InvalidMessageBusClientIndex;
            }

            client.UpdateEvent += new MessageBusClient.ClientUpdateDelegate(client_UpdateEvent);

            RaiseClientAddedEvent(clientId);

            return index;
        }

        protected virtual void client_UpdateEvent(MessageBusClient client)
        {
            RaiseClientUpdateEvent(client.Id);
        }

        /// <summary>
        /// Add a client to the message bus.
        /// </summary>
        public override bool AddClient(MessageBusClient client)
        {
            if (client == null)
            {
                return false;
            }

            if (client.ExecutionStrategy == null)
            {// Assign the client an instance of the default type of execution strategy used.
                client.SetupExecutionStrategy(new ThreadPoolFastExecutionStrategy(true));
            }

            if (DoAddClient(client, client.Id) == ClientId.InvalidMessageBusClientIndex)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Remove a client from the bus.
        /// </summary>
        public override bool RemoveClient(MessageBusClient client, bool isPermanent)
        {
            int id = client.Id.LocalMessageBusIndex;

            lock (_syncRoot)
            {
                if (_clientsHotSwap.Count <= id || id < 0)
                {
#if Matrix_Diagnostics
                    SystemMonitor.OperationError("Failed to remove client from message bus.");
#endif
                    return false;
                }

                if (_clientsHotSwap[id] != client)
                {
#if Matrix_Diagnostics
                    SystemMonitor.OperationError("Client [" + client.Id.ToString() + "] not removed, since it does not belong to this message bus with this ID.");
#endif
                    return false;
                }

                // Performs an internal hot swap.
                _clientsHotSwap[id] = null;

                // Performs an internal hot swap.
                _guidToIndexHotSwap[client.Id.Guid] = ClientId.InvalidMessageBusClientIndex;
            }

            client.UpdateEvent -= new MessageBusClient.ClientUpdateDelegate(client_UpdateEvent);
            client.ReleaseMessageBus();

            RaiseClientRemovedEvent(client.Id, isPermanent);

            return true;
        }

        /// <summary>
        /// Actually supply the item to client. 
        /// </summary>
        /// <param name="senderId"></param>
        /// <param name="receiverId"></param>
        /// <param name="envelope"></param>
        /// <param name="requestConfirm">In local mode we have receival confirmation, so this value is ignored here (as result is always assured true).</param>
        /// <returns></returns>
        protected virtual SendToClientResultEnum DoSendToClient(ClientId senderId, ClientId receiverId,
                                                                Envelope envelope, TimeSpan? requestConfirmTimeout)
        {
            if (receiverId.MessageBus != this)
            {
                //// Maybe this is a "lost" id, try to see if it is one of ours.
                //if (receiverId.MessageBus == null && _guidToIndexHotSwap.ContainsKey(receiverId.Guid))
                //{// Yes!
                //    receiverId.MessageBus = this;
                //}
                return SendToClientResultEnum.ClientNotFound;
            }

            MessageBusClient client = GetLocalClientByIndex(receiverId.LocalMessageBusIndex);
            if (client == null)
            {
                return SendToClientResultEnum.ClientNotFound;
            }

            ISerializer serializer = _serializer;
            if (serializer == null)
            {
                return SendToClientResultEnum.Failure;
            }

            // Duplicate what (if anything) as according to envelope duplication model.
            envelope = envelope.Duplicate(serializer);
            envelope.History.PushStamp(new EnvelopeStamp(PendingStampId, receiverId, senderId));

            if (client.Receive(envelope))
            {
                return SendToClientResultEnum.Success;
            }
            else
            {
                return SendToClientResultEnum.Failure;
            }
        }

        /// <summary>
        /// Send an item to multiple recipients.
        /// </summary>
        protected override Outcomes DoSend(ClientId senderId, IEnumerable<ClientId> receiversIds, 
                                       Envelope envelope, TimeSpan? requestConfirmTimeout, bool showErrorsDiagnostics)
        {
            if (IsDisposed)
            {// Possible to get disposed while operating here.
                return Outcomes.SystemFailture;
            }

            //if (envelope.Address != null)
            //{
            //    SystemMonitor.OperationError("Envelope transport direction not clear.");
            //    return false;
            //}

            bool result = true;
            foreach (ClientId receiverId in receiversIds)
            {
                if (DoSendToClient(senderId, receiverId, envelope, requestConfirmTimeout) != SendToClientResultEnum.Success)
                {
                    result = false;
                }
            }

            return result ? Outcomes.Success : Outcomes.Failure;
        }

    }
}
