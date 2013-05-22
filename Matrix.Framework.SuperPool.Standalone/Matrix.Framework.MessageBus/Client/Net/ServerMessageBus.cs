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
using Matrix.Common.Core;
using Matrix.Common.Core.Collections;
using Matrix.Common.Extended;
using Matrix.Common.Sockets.Common;
using Matrix.Common.Sockets.Core;
using Matrix.Framework.MessageBus.Core;
using Matrix.Framework.MessageBus.Net.Messages;
using System.Threading;
using System.Net;

#if Matrix_Diagnostics
using Matrix.Common.Diagnostics.TracerCore.Items;
#endif

namespace Matrix.Framework.MessageBus.Net
{
    /// <summary>
    /// Extends the default message bus functionality with networking capabilities.
    /// The message bus serves as a server for messages.
    /// </summary>
    public class ServerMessageBus : Matrix.Framework.MessageBus.Core.MessageBus
    {
        SocketMessageServer _server = null;

        /// <summary>
        /// What socketId has what clients.
        /// </summary>
        BiDictionary<int, ListUnique<ClientId>> _remoteClientsNetIds = new BiDictionary<int, ListUnique<ClientId>>();

        /// <summary>
        /// Access control information for each client. If null means client has not provided any.
        /// </summary>
        Dictionary<int, ClientAccessControl> _clientsAccessControl = new Dictionary<int, ClientAccessControl>();

        /// <summary>
        /// What client belongs to what socketId.
        /// </summary>
        Dictionary<ClientId, int> _remoteClientNetId = new Dictionary<ClientId, int>();

        /// <summary>
        /// What remote client implements what type.
        /// </summary>
        Dictionary<ClientId, Type> _remoteClientsTypes = new Dictionary<ClientId, Type>();

        /// <summary>
        /// What remove client source is of what type.
        /// </summary>
        Dictionary<ClientId, List<string>> _remoteClientsSourcesTypesNames = new Dictionary<ClientId, List<string>>();

        int _pendingMessageId = 0;
        protected int PendingMessageId
        {
            get { return Interlocked.Increment(ref _pendingMessageId); }
        }

        /// <summary>
        /// The default port of the server to accept incoming connections on.
        /// </summary>
        public const int DefaultPort = 18261;

        /// <summary>
        /// Access control rules.
        /// </summary>
        public ServerAccessControl AccessControl { get; protected set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="port">The port for the server of the bus. Leave null for default port.</param>
        /// <param name="accessControl">Control requirements for user and password for connecting clients, pass null for no control (everyone can connect).</param>
        public ServerMessageBus(string name, int? port, ServerAccessControl accessControl)
            : base(name)
        {
            AccessControl = accessControl;
            if (port.HasValue == false)
            {
                port = DefaultPort;
            }

            _server = new SocketMessageServer(base.Serializer);
            _server.Start(new IPEndPoint(IPAddress.Any, port.Value));

            _server.ClientAsyncMessageSendEvent += new SocketMessageServer.AsyncMessageSendUpdateDelegate(_server_ClientAsyncMessageSendEvent);
            _server.ClientConnectedEvent += new SocketMessageServer.ServerClientUpdateDelegate(_server_ClientConnectedEvent);
            _server.ClientDisconnectedEvent += new SocketMessageServer.ServerClientUpdateDelegate(_server_ClientDisconnectedEvent);
            _server.ClientMessageReceivedEvent += new SocketMessageServer.MessageUpdateDelegate(_server_ClientMessageReceivedEvent);

            ApplicationLifetimeHelper.ApplicationClosingEvent += new CommonHelper.DefaultDelegate(ApplicationLifetimeHelper_ApplicationClosingEvent);
        }

        void ApplicationLifetimeHelper_ApplicationClosingEvent()
        {
            SocketMessageServer server = _server;
            if (server != null)
            {
                server.SendAsync(new StateUpdateMessage()
                                     {
                                         MessageId = PendingMessageId,
                                         State = StateUpdateMessage.StateEnum.Shutdown,
                                         RequestResponse = false
                                     }, null);

                // Allow a little time for the status update message(s) to travel.
                Thread.Sleep(150);
            }
        }

        public override void Dispose()
        {
            ApplicationLifetimeHelper.ApplicationClosingEvent -= new CommonHelper.DefaultDelegate(ApplicationLifetimeHelper_ApplicationClosingEvent);

            SocketMessageServer server = _server;
            _server = null;

            if (server != null)
            {
                server.ClientAsyncMessageSendEvent -= new SocketMessageServer.AsyncMessageSendUpdateDelegate(_server_ClientAsyncMessageSendEvent);
                server.ClientConnectedEvent -= new SocketMessageServer.ServerClientUpdateDelegate(_server_ClientConnectedEvent);
                server.ClientDisconnectedEvent -= new SocketMessageServer.ServerClientUpdateDelegate(_server_ClientDisconnectedEvent);
                server.ClientMessageReceivedEvent -= new SocketMessageServer.MessageUpdateDelegate(_server_ClientMessageReceivedEvent);

                server.Stop(TimeSpan.FromSeconds(2));
                server.Dispose();
            }

            base.Dispose();
        }

        bool ToClient(int clientSocketId, Message message, TimeSpan? requestConfirmTimeout)
        {
            SocketMessageServer server = _server;
            if (server == null)
            {
                return false;
            }

            ServerAccessControl accessControl = AccessControl;
            if (accessControl != null)
            {
                if (accessControl.IsAllowed(ObtainClientAccessControl(clientSocketId)) == false)
                {
#if Matrix_Diagnostics
                    InstanceMonitor.OperationWarning("Message [" + message.ToString() + "] was not sent to client [" + clientSocketId + "] due to access control.");
#endif
                    return false;
                }
            }

            message.MessageId = PendingMessageId;
            return server.SendAsync(clientSocketId, message, requestConfirmTimeout);
        }

        public override bool ContainsClient(ClientId clientId)
        {
            if (base.ContainsClient(clientId) == false)
            {
                lock (_syncRoot)
                {
                    return _remoteClientNetId.ContainsKey(clientId);
                }
            }

            return true;
        }

        public override Type GetClientType(ClientId clientId)
        {
            if (clientId.IsMessageBusIndexValid && clientId.MessageBus == this)
            {// Receiver seems to be a local item.
                return base.GetClientType(clientId);
            }

            lock (_syncRoot)
            {
                Type value;
                if (this._remoteClientsTypes.TryGetValue(clientId, out value))
                {
                    return value;
                }
            }

            return null;
        }

        public override List<ClientId> GetAllClientsIds()
        {
            List<ClientId> result = base.GetAllClientsIds();
            
            lock(_syncRoot)
            {
                foreach(ClientId id in _remoteClientNetId.Keys)
                {
                    result.Add(id);
                }
            }

            return result;
        }

        public override List<string> GetClientSourceTypes(ClientId clientId)
        {
            if (clientId.IsMessageBusIndexValid && clientId.MessageBus == this)
            {// Receiver seems to be a local item.
                return base.GetClientSourceTypes(clientId);
            }

            lock (_syncRoot)
            {
                List<string> names;
                if (this._remoteClientsSourcesTypesNames.TryGetValue(clientId, out names))
                {
                    return names;
                }
            }

            return null;
        }

        protected override void client_UpdateEvent(MessageBusClient client)
        {
            base.client_UpdateEvent(client);

            // Also send this notification to clients.
            int[] keys;
            lock (_syncRoot)
            {
                keys = GeneralHelper.EnumerableToArray<int>(_remoteClientsNetIds.Keys);
            }

            ClientUpdateMessage message = new ClientUpdateMessage() { 
                                                                        ClientId = client.Id, MessageId = PendingMessageId, RequestResponse = false };

            foreach (int key in keys)
            {
                ToClient(key, message, null);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="senderId"></param>
        /// <param name="receiverId"></param>
        /// <param name="envelope"></param>
        /// <param name="requestConfirm">Only valid for remote clients, since all local calls are confirmed or denied by default.</param>
        /// <returns></returns>
        protected override SendToClientResultEnum DoSendToClient(ClientId senderId, ClientId receiverId, 
                                                                 Envelope envelope, TimeSpan? requestConfirmTimeout)
        {
            if (receiverId.IsMessageBusIndexValid && (receiverId.MessageBus == this))
                //|| receiverId.MessageBus == null)) // This allows for "lost" ids.
            {// Receiver seems to be a local item.
                SendToClientResultEnum result = base.DoSendToClient(senderId, receiverId, envelope, requestConfirmTimeout);
                if (result != SendToClientResultEnum.ClientNotFound)
                {
                    return result;
                }
            }

            int clientSocketId = 0;
            lock (_syncRoot)
            {
                if (_remoteClientNetId.TryGetValue(receiverId, out clientSocketId) == false)
                {
                    return SendToClientResultEnum.ClientNotFound;
                }
            }

            // Send message.
            EnvelopeMessage message = new EnvelopeMessage() { Envelope = envelope,
                                                              Receivers = new ClientId[] { receiverId },
                                                              Sender = senderId,
                                                              RequestResponse = false
                                                            };

            if (ToClient(clientSocketId, message, requestConfirmTimeout) == false)
            {
                return SendToClientResultEnum.Failure;
            }
            else
            {
                return SendToClientResultEnum.Success;
            }
        }

        void RegisterClientId(int socketClientId, ClientId id, Type type, List<string> sourceTypeNames)
        {
            // Remove the index, that was valid for the remote bus, we use this as pure Id only.
            id.LocalMessageBusIndex = ClientId.InvalidMessageBusClientIndex;

            bool newlyAdded = false;
            lock (_syncRoot)
            {
                // Update all of the remote client state collections.
                newlyAdded = (_remoteClientNetId.ContainsKey(id) == false);
                
                _remoteClientNetId[id] = socketClientId;
                _remoteClientsTypes[id] = type;
                _remoteClientsSourcesTypesNames[id] = sourceTypeNames;

                if (_remoteClientsNetIds.ContainsKey(socketClientId) == false)
                {
                    _remoteClientsNetIds.Add(socketClientId, new ListUnique<ClientId>());
                }

                _remoteClientsNetIds[socketClientId].Add(id);
            }

            if (newlyAdded)
            {
                RaiseClientAddedEvent(id);
            }
        }

        ClientAccessControl ObtainClientAccessControl(int clientId)
        {
            if (clientId < 0)
            {
                return null;
            }

            ClientAccessControl result;
            lock (_syncRoot)
            {
                if (_clientsAccessControl.TryGetValue(clientId, out result) == false)
                {
                    result = new ClientAccessControl();
                    _clientsAccessControl.Add(clientId, result);
                }
            }

            return result;
        }

        void _server_ClientMessageReceivedEvent(SocketMessageServer server, SocketCommunicatorEx client, object message)
        {
            ServerAccessControl accessControl = AccessControl;
            // Check security first.
            if (accessControl != null && message is AccessMessage == false)
            {
                if (accessControl.IsAllowed(ObtainClientAccessControl(client.Id)) == false)
                {
#if Matrix_Diagnostics
                    InstanceMonitor.Info("Message [" + message.ToString()  +"] from client [" + client.ToString() + "] not allowed due to access control.", TracerItem.PriorityEnum.Medium);
#endif
                    return;
                }
            }

            if (message is EnvelopeMessage)
            {// Envelope user message.
                
                EnvelopeMessage envelopeMessage = (EnvelopeMessage)message;

                // Remove the remote message bus index association.
                envelopeMessage.Sender.LocalMessageBusIndex = ClientId.InvalidMessageBusClientIndex;

                foreach (ClientId id in envelopeMessage.Receivers)
                {
                    // Assign the id as local id, if it is, otherwise skip it.
                    id.LocalMessageBusIndex = base.GetClientIndexByGuid(id.Guid);
                    if (id.IsMessageBusIndexValid)
                    {
                        id.MessageBus = this;
                        if (DoSendToClient(envelopeMessage.Sender, id, envelopeMessage.Envelope, null) != SendToClientResultEnum.Success)
                        {
#if Matrix_Diagnostics
                            InstanceMonitor.OperationError(string.Format("Failed to accept envelope message [{0}].", envelopeMessage.ToString()));
#endif
                        }
                    }
                    else
                    {
#if Matrix_Diagnostics
                        InstanceMonitor.OperationError(string.Format("Failed to accept envelope message [{0}] due unrecognized receiver id.", envelopeMessage.ToString()));
#endif
                    }

                }
            }
            else if (message is ClientsListMessage)
            {// Message bus system message.

                ClientsListMessage updateMessage = (ClientsListMessage)message;
                for (int i = 0; i < updateMessage.Ids.Count; i++)
                {
                    RegisterClientId(client.Id, updateMessage.Ids[i], updateMessage.Types[i], updateMessage.SourcesTypes[i]);
                }

            }
            else if (message is RequestClientListUpdateMessage)
            {
                SendClientsUpdate(client.Id);
            }
            else if (message is ClientUpdateMessage)
            {
                ClientUpdateMessage updateMessage = (ClientUpdateMessage)message;
                
                bool validClient;
                lock (_syncRoot)
                {
                    validClient = _remoteClientNetId.ContainsKey(updateMessage.ClientId);
                }

                if (validClient)
                {
                    RaiseClientAddedEvent(updateMessage.ClientId);
                }
                else
                {
#if Matrix_Diagnostics
                    InstanceMonitor.OperationError(string.Format("Failed to raise update event for client [{0}], since client not found.", updateMessage.ClientId.ToString()));
#endif
                }
            }
            else if (message is AccessMessage)
            {
                ClientAccessControl control = ObtainClientAccessControl(client.Id);
                if (control != null)
                {
                    control.Update(message as AccessMessage);
                }
            }
            else if (message is StateUpdateMessage)
            {
                RaiseCounterPartyUpdateEvent("Client:" + client.Id.ToString(), ((StateUpdateMessage)message).State.ToString());
            }
            else
            {
#if Matrix_Diagnostics
                InstanceMonitor.Warning(string.Format("Message [{0}] not recognized.", message.GetType().Name));
#endif
            }
        }

        void _server_ClientDisconnectedEvent(SocketMessageServer server, SocketCommunicatorEx client)
        {
            // Clear all clients hooked on this connection.
            ListUnique<ClientId> clientsIds = null;
            lock (_syncRoot)
            {
                if (_remoteClientsNetIds.TryGetByKey(client.Id, ref clientsIds) == false)
                {
                    return;
                }

                _clientsAccessControl.Remove(client.Id);

                _remoteClientsNetIds.RemoveByKey(client.Id);
                foreach (ClientId id in clientsIds)
                {
                    _remoteClientNetId.Remove(id);
                    _remoteClientsTypes.Remove(id);
                    _remoteClientsSourcesTypesNames.Remove(id);
                }
            }

            // Raise event to notify of the disconnection of all these Ids.
            foreach (ClientId id in clientsIds)
            {
                // Notify of clients removal, with non permanent remove, since they may later be restored.
                RaiseClientRemovedEvent(id, false);
            }
        }

        /// <summary>
        /// Helper, sends an update with all the *local* clients ids to the server.
        /// </summary>
        bool SendClientsUpdate(int socketId)
        {
            Console.WriteLine("Send clients update: "+socketId);
            ClientsListMessage message = new ClientsListMessage();
            foreach (MessageBusClient client in Clients)
            {
                message.Ids.Add(client.Id);
                message.AddType(client.GetType(), client.OptionalSourceType);
            }
            lock(_syncRoot){
                foreach (ClientId id in _remoteClientsTypes.Keys)
                {
                    //Console.WriteLine("Client sent: "+id.ToString());
                    //if (_remoteClientsNetIds.ContainsKey(socketId) && _remoteClientsNetIds[socketId].Contains(id))
                    //    continue;
                    message.Ids.Add(id);
                    message.AddType(_remoteClientsTypes[id], null);
                }
            }
            foreach(var sourceTypes in message.SourcesTypes)
            {
                if (sourceTypes == null)
                    continue;
                foreach(string type in sourceTypes)
                {
                    Console.WriteLine("Source type: "+type);
                }
            }

            return ToClient(socketId, message, null);
        }

        void _server_ClientConnectedEvent(SocketMessageServer server, SocketCommunicatorEx client)
        {
            // The client expects is, so activate keep alive.
            client.KeepAlive = true;

            // Send local bus clients info to connected element.
            // Important - this will also be sent even to clients that do not have verified access control.
            if (SendClientsUpdate(client.Id) == false)
            {
#if Matrix_Diagnostics
                InstanceMonitor.OperationError("Failed to send clients update to client [" + client.ToString() + ", " + client.Id + "].");
#endif
            }
        }

        void _server_ClientAsyncMessageSendEvent(SocketMessageServer server, SocketCommunicatorEx client, SocketCommunicator.AsyncMessageSendInfo info)
        {
            // A message has been successfully sent to client.
        }

    }
}
