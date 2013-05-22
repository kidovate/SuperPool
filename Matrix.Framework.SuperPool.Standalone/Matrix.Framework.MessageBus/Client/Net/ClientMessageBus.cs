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
using System.Net;
using System.Threading;
using Matrix.Common.Core;
using Matrix.Common.Core.Collections;
using Matrix.Common.Sockets.Common;
using Matrix.Common.Sockets.Core;
using Matrix.Framework.MessageBus.Core;
using Matrix.Framework.MessageBus.Net.Messages;

namespace Matrix.Framework.MessageBus.Net
{
    /// <summary>
    /// Extends the message bus to allow it to connect to a servering message bus.
    /// </summary>
    public class ClientMessageBus : global::Matrix.Framework.MessageBus.Core.MessageBus
    {
        SocketMessageClient _socketClient;

        int _pendingMessageId = 0;
        protected int PendingMessageId
        {
            get { return Interlocked.Increment(ref _pendingMessageId); }
        }

        public ClientAccessControl AccessControl { get; protected set; }

        HotSwapDictionary<Guid, ClientId> _originalServerClientsHotSwap = new HotSwapDictionary<Guid, ClientId>();
        HotSwapDictionary<Guid, Type> _originalServerClientsTypesHotSwap = new HotSwapDictionary<Guid, Type>();
        HotSwapDictionary<Guid, List<string>> _originalServerClientsSourcesTypesHotNamesSwap = new HotSwapDictionary<Guid, List<string>>();

        public bool IsConnected
        {
            get
            {
                SocketMessageClient socketClient = _socketClient;
                if (socketClient == null)
                {
                    return false;
                }

                return socketClient.IsConnected;
            }
        }

        public delegate void MessageUpdateDelegate(ClientMessageBus client, object message);
        public event MessageUpdateDelegate MessageUpdateEvent;


        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="nameAppending">Additional part to the default named bus (name is {endpoint} + "MessageBus.Proxy" + {appending}).</param>
        /// <param name="accessControl">Control acces to remote server (user and pass if required), pass null for no login.</param>
        public ClientMessageBus(IPEndPoint endpoint, string nameAppendix, ClientAccessControl accessControl)
            : base(endpoint.ToString() + " MessageBus.Proxy " + nameAppendix)
        {
            AccessControl = accessControl;

            _socketClient = new SocketMessageClient(endpoint, base.Serializer);

            _socketClient.ConnectedEvent += new SocketCommunicator.HelperUpdateDelegate(_messageClient_ConnectedEvent);
            _socketClient.DisconnectedEvent += new SocketCommunicator.HelperUpdateDelegate(_messageClient_DisconnectedEvent);
            _socketClient.MessageReceivedEvent += new SocketCommunicator.MessageUpdateDelegate(_messageClient_MessageReceivedEvent);
            _socketClient.SendAsyncCompleteEvent += new SocketCommunicator.AsyncMessageSendDelegate(_messageClient_SendAsyncCompleteEvent);

            _socketClient.AutoReconnect = true;
            _socketClient.KeepAlive = true;

            base.ClientAddedEvent += new global::Matrix.Framework.MessageBus.Core.MessageBusClientUpdateDelegate(MessageBusNetClient_ClientAddedEvent);
            base.ClientRemovedEvent += new global::Matrix.Framework.MessageBus.Core.MessageBusClientRemovedDelegate(MessageBusNetClient_ClientRemovedEvent);

            ApplicationLifetimeHelper.ApplicationClosingEvent += new CommonHelper.DefaultDelegate(ApplicationLifetimeHelper_ApplicationClosingEvent);
        }

        void ApplicationLifetimeHelper_ApplicationClosingEvent()
        {
            ToServer(new StateUpdateMessage() { 
                                                  MessageId = PendingMessageId, State = StateUpdateMessage.StateEnum.Shutdown, RequestResponse = false }, null);

            Thread.Sleep(100);
        }

        public override void Dispose()
        {
            ApplicationLifetimeHelper.ApplicationClosingEvent -= new CommonHelper.DefaultDelegate(ApplicationLifetimeHelper_ApplicationClosingEvent); 
            base.ClientAddedEvent -= new global::Matrix.Framework.MessageBus.Core.MessageBusClientUpdateDelegate(MessageBusNetClient_ClientAddedEvent);
            base.ClientRemovedEvent -= new global::Matrix.Framework.MessageBus.Core.MessageBusClientRemovedDelegate(MessageBusNetClient_ClientRemovedEvent);

            base.Dispose();
            
            SocketMessageClient messageClient = _socketClient;
            _socketClient = null;

            if (messageClient != null)
            {
                messageClient.ConnectedEvent -= new SocketCommunicator.HelperUpdateDelegate(_messageClient_ConnectedEvent);
                messageClient.DisconnectedEvent -= new SocketCommunicator.HelperUpdateDelegate(_messageClient_DisconnectedEvent);
                messageClient.MessageReceivedEvent -= new SocketCommunicator.MessageUpdateDelegate(_messageClient_MessageReceivedEvent);
                messageClient.SendAsyncCompleteEvent -= new SocketCommunicator.AsyncMessageSendDelegate(_messageClient_SendAsyncCompleteEvent);

                messageClient.Dispose();
            }
        }

        void MessageBusNetClient_ClientAddedEvent(global::Matrix.Framework.MessageBus.Core.IMessageBus messageBus, ClientId clientId)
        {
            SendClientsUpdate();
        }

        void MessageBusNetClient_ClientRemovedEvent(global::Matrix.Framework.MessageBus.Core.IMessageBus messageBus, ClientId clientId, bool isPermanenet)
        {
            SendClientsUpdate();
        }

        void _messageClient_SendAsyncCompleteEvent(SocketCommunicator helper, SocketCommunicator.AsyncMessageSendInfo info)
        {
            // Message sent to server.
            MessageUpdateDelegate del = MessageUpdateEvent;
            if (del != null)
            {
                del(this, info.Message);
            }
        }

        void _messageClient_MessageReceivedEvent(SocketCommunicator helper, object message)
        {
            if (message is EnvelopeMessage)
            {
                EnvelopeMessage envelopeMessage = (EnvelopeMessage)message;

                // Remove the remote message bus index association.
                envelopeMessage.Sender.LocalMessageBusIndex = ClientId.InvalidMessageBusClientIndex;

                foreach (ClientId id in envelopeMessage.Receivers)
                {
                    // Decode the id.
                    id.LocalMessageBusIndex = base.GetClientIndexByGuid(id.Guid);

                    if (id.IsMessageBusIndexValid)
                    {
                        // Assign as a part of the local bus.
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
                        InstanceMonitor.OperationError(string.Format("Failed to accept envelope message [{0}] due to unrecognized receiver id.", envelopeMessage.ToString()));
#endif
                    }

                    
                }
            }
            else if (message is ClientsListMessage)
            {// Received client update from server.

                ClientsListMessage listMessage = (ClientsListMessage)message;

                int jef = 0;
                foreach(var client in listMessage.Ids)
                {
                    Console.WriteLine("Incoming client id: "+client+" Source type: "+listMessage.SourcesTypes[jef]);
                    jef++;
                }

                List<ClientId> existingIds = new List<ClientId>();
                lock (_syncRoot)
                {
                    existingIds.AddRange(_originalServerClientsHotSwap.Values);

                    _originalServerClientsHotSwap.Clear();
                    _originalServerClientsTypesHotSwap.Clear();
                    _originalServerClientsSourcesTypesHotNamesSwap.Clear();

                    // Preprocess Ids, by assigning them new indeces and adding to the local message bus register.
                    for (int i = 0; i < listMessage.Ids.Count; i++)
                    {
                        // Add an original copy to the list.
                        _originalServerClientsHotSwap.Add(listMessage.Ids[i].Guid, listMessage.Ids[i]);

                        _originalServerClientsTypesHotSwap.Add(listMessage.Ids[i].Guid, listMessage.Types[i]);
                        _originalServerClientsSourcesTypesHotNamesSwap.Add(listMessage.Ids[i].Guid, listMessage.SourcesTypes[i]);

                        // Add the client to a new spot.
                        //_clientsHotSwap.Add(null);
                        //int messageBusIndex = _clientsHotSwap.Count - 1;

                        // This type of assignment will also work with multiple entries.
                        // This performs an internal hotswap.
                        //_guidToIndexHotSwap[id.Guid] = messageBusIndex;

                        // Also add to this classes collection.
                        //_localToRemoteId[messageBusIndex] = id;
                    }
                }

                foreach (ClientId id in listMessage.Ids)
                {
                    existingIds.Remove(id);
                    RaiseClientAddedEvent(id);
                }

                // Raise for any that were removed.
                foreach (ClientId id in existingIds)
                {
                    RaiseClientRemovedEvent(id, true);
                }
            }
            else if (message is RequestClientListUpdateMessage)
            {
                SendClientsUpdate();
            }
            else if (message is ClientUpdateMessage)
            {
                ClientUpdateMessage updateMessage = (ClientUpdateMessage)message;

                if (_originalServerClientsHotSwap.ContainsKey(updateMessage.ClientId.Guid))
                {
                    RaiseClientUpdateEvent(updateMessage.ClientId);
                }
                else
                {
#if Matrix_Diagnostics
                    InstanceMonitor.OperationError(string.Format("Failed to raise update event for client [{0}], since client not found.", updateMessage.ClientId.ToString()));
#endif
                }
            }
            else if (message is StateUpdateMessage)
            {
                RaiseCounterPartyUpdateEvent("Server", ((StateUpdateMessage)message).State.ToString());
            }
            else
            {
#if Matrix_Diagnostics
                InstanceMonitor.Warning(string.Format("Message [{0}] not recognized.", message.GetType().Name));
#endif
            }
        }

        void _messageClient_DisconnectedEvent(SocketCommunicator helper)
        {
            ICollection<ClientId> ids = _originalServerClientsHotSwap.Values;
            _originalServerClientsHotSwap.Clear();

            //lock (_syncRoot)
            //{
            //    _localToRemoteId.Clear();
            //}

            // Removing all server clients.
            foreach (ClientId id in ids)
            {
                // Notify of clients removal, with non permanent remove, since they may later be restored.
                RaiseClientRemovedEvent(id, false);
            }
        }

        void _messageClient_ConnectedEvent(SocketCommunicator helper)
        {
            // Send an update of the clients to server.
            if (_socketClient == helper)
            {
                SendAccessControlMessage();

                SendClientsUpdate();
            }
        }

        bool SendAccessControlMessage()
        {
            ClientAccessControl accessControl = AccessControl;
            if (accessControl == null)
            {
                return true;
            }

            return ToServer(accessControl.ObtainClientSideMessage(), null);
        }

        /// <summary>
        /// Helper, sends an update with all the local clients ids to the server.
        /// </summary>
        bool SendClientsUpdate()
        {
            ClientsListMessage message = new ClientsListMessage();
            foreach (MessageBusClient client in Clients)
            {
                message.Ids.Add(client.Id);
                message.AddType(client.GetType(), client.OptionalSourceType);
            }

            return ToServer(message, null);
        }

        /// <summary>
        /// Helper, send message to server.
        /// </summary>
        bool ToServer(Message message, TimeSpan? requestConfirmTimeout)
        {
            SocketMessageClient messageClient = _socketClient;
            if (messageClient == null)
            {
                return false;
            }

            message.MessageId = PendingMessageId;
            return messageClient.SendAsync(message, requestConfirmTimeout) != SocketCommunicator.InvalidSendIndex;
        }

        public override List<ClientId> GetAllClientsIds()
        {
            List<ClientId> result = base.GetAllClientsIds();
            foreach (KeyValuePair<Guid, ClientId> pair in _originalServerClientsHotSwap)
            {
                if (pair.Value != null)
                {
                    result.Add(pair.Value);
                }
            }

            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        public override bool ContainsClient(ClientId clientId)
        {
            if (base.ContainsClient(clientId))
            {
                return true;
            }

            // Check agains the guid, since the Id instance may be different (also with different message bus id).
            return _originalServerClientsHotSwap.ContainsKey(clientId.Guid);
        }

        protected override void client_UpdateEvent(MessageBusClient client)
        {
            base.client_UpdateEvent(client);
            
            // Also send this notification to server.
            ToServer(new ClientUpdateMessage() { ClientId = client.Id, MessageId = PendingMessageId, RequestResponse = false }, null);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="requestConfirm">Only valid for remote clients, since all local calls are confirmed or denied by default.</param>
        protected override SendToClientResultEnum DoSendToClient(ClientId senderId, ClientId receiverId, 
                                                                 Envelope envelope, TimeSpan? requestConfirmTimeout)
        {
            if (receiverId.IsMessageBusIndexValid && receiverId.MessageBus == this)
            {// Seems to be a local client Id.
                SendToClientResultEnum result = base.DoSendToClient(senderId, receiverId, envelope, requestConfirmTimeout);
                if (result != SendToClientResultEnum.ClientNotFound)
                {
                    return result;
                }
            }

            // Receiver was not local in parrent, try remote.
            if (IsConnected == false)
            {
                return SendToClientResultEnum.Failure;
            }

            EnvelopeMessage message = new EnvelopeMessage()
                                          {
                                              Envelope = envelope,
                                              Receivers = new ClientId[] { receiverId },
                                              Sender = senderId
                                          };

            return ToServer(message, requestConfirmTimeout) ? SendToClientResultEnum.Success : SendToClientResultEnum.Failure;

        }

        /// <summary>
        /// This will transport the envelope to the server, if it can.
        /// </summary>
        protected override Outcomes DoSend(ClientId senderId, IEnumerable<ClientId> receiversIds,
                                       Envelope envelope, TimeSpan? requestConfirmTimeout, bool showErrorsDiagnostics)
        {
            bool result = true;
            foreach (ClientId id in receiversIds)
            {
                result = result && DoSendToClient(senderId, id, envelope, requestConfirmTimeout) == SendToClientResultEnum.Success;
            }

            return result ? Outcomes.Success : Outcomes.Failure;
        }

        public override Type GetClientType(ClientId clientId)
        {
            if (clientId.IsMessageBusIndexValid && clientId.MessageBus == this)
            {// Seems to be a local client Id.
                return base.GetClientType(clientId);
            }

            Type value;
            if (_originalServerClientsTypesHotSwap.TryGetValue(clientId.Guid, out value))
            {
                return value;
            }

            return null;
        }

        public override List<string> GetClientSourceTypes(ClientId clientId)
        {
            if (clientId.IsMessageBusIndexValid && clientId.MessageBus == this)
            {// Seems to be a local client Id.
                return base.GetClientSourceTypes(clientId);
            }

            List<string> names;
            if (_originalServerClientsSourcesTypesHotNamesSwap.TryGetValue(clientId.Guid, out names))
            {
                return names;
            }

            return null;
        }
    }
}
