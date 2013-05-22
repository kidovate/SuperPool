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
using System.Net.Sockets;
using System.Net;
using System.Threading;
using Matrix.Common.Core;
using Matrix.Common.Core.Collections;
using Matrix.Common.Core.Serialization;
using Matrix.Common.Sockets.Common;

#if Matrix_Diagnostics
    using Matrix.Common.Diagnostics;
#endif

namespace Matrix.Common.Sockets.Core
{
    /// <summary>
    /// TCP socket based message server implementation.
    /// </summary>
    public class SocketMessageServer : IDisposable
    {
        object _syncRoot = new object();

        public const int DefaultPort = 11632;

        int _backlog = 128;
        /// <summary>
        /// Connections backlog.
        /// </summary>
        public int Backlog
        {
            get { return _backlog; }
            set { _backlog = value; }
        }


        volatile System.Net.Sockets.Socket _listenSocket;

        /// <summary>
        /// Is the server started and listening.
        /// </summary>
        public bool IsStarted
        {
            get 
            { 
                return _listenSocket != null && _listenSocket.IsBound; 
            }
        }

        HotSwapDictionary<int, SocketCommunicatorEx> _clientsHotSwap = new HotSwapDictionary<int, SocketCommunicatorEx>();
        
        /// <summary>
        /// An enumerable containing all the clients pairs.
        /// </summary>
        public IEnumerable<KeyValuePair<int, SocketCommunicatorEx>> Clients
        {
            get { return _clientsHotSwap; }
        }

        /// <summary>
        /// An enumerable containing only the clients instances.
        /// </summary>
        public IEnumerable<SocketCommunicatorEx> ClientsOnly
        {
            get { return _clientsHotSwap.Values; }
        }

        /// <summary>
        /// The serializer used to serialize and deserialize messages to byte[].
        /// </summary>
        ISerializer _serializer;

        int _pendingClientId = 0;
        protected int PendingClientId
        {
            get { return Interlocked.Increment(ref _pendingClientId); }
        }

#if Matrix_Diagnostics
        InstanceMonitor _monitor;
        public InstanceMonitor Monitor
        {
            get { return _monitor; }
        }
#endif

        public delegate void ServerClientUpdateDelegate(SocketMessageServer server, SocketCommunicatorEx client);
        public delegate void MessageUpdateDelegate(SocketMessageServer server, SocketCommunicatorEx client, object message);
        public delegate void AsyncMessageSendUpdateDelegate(SocketMessageServer server, SocketCommunicatorEx client, SocketCommunicator.AsyncMessageSendInfo info);

        public event ServerClientUpdateDelegate ClientConnectedEvent;
        public event ServerClientUpdateDelegate ClientDisconnectedEvent;

        public event MessageUpdateDelegate ClientMessageReceivedEvent;
        public event AsyncMessageSendUpdateDelegate ClientAsyncMessageSendEvent;

        /// <summary>
        /// Constructor.
        /// </summary>
        public SocketMessageServer(ISerializer serializer)
        {
#if Matrix_Diagnostics
            _monitor = new InstanceMonitor(this);
#endif
            _serializer = serializer;
        }

        /// <summary>
        /// Dispose.
        /// </summary>
        public void Dispose()
        {
            // Stop the main accept socket.
            Stop(null);

            ClientConnectedEvent = null;
            ClientDisconnectedEvent = null;

            ClientMessageReceivedEvent = null;
            ClientAsyncMessageSendEvent = null;

            // Dispose all clients.
            SocketCommunicatorEx[] clients = CommonHelper.EnumerableToArray<SocketCommunicatorEx>(_clientsHotSwap.Values);
            _clientsHotSwap.Clear();

            _serializer = null;

            foreach (SocketCommunicatorEx client in clients)
            {
                client.Dispose();
            }

        }

        /// <summary>
        /// Start the server.
        /// </summary>
        public bool Start(IPEndPoint endPoint)
        {
            lock (_syncRoot)
            {
                if (_listenSocket != null)
                {// Already started.
#if Matrix_Diagnostics
                    Monitor.OperationWarning("Server already started.");
#endif
                    return true;
                }

                try
                {
                    this._listenSocket = new System.Net.Sockets.Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    this._listenSocket.Bind(endPoint);
                    this._listenSocket.Listen(_backlog);
                }
                catch (SocketException ex)
                {
#if Matrix_Diagnostics
                    Monitor.OperationError("Failed to start server.", ex);
#endif
                    _listenSocket.Close();
                    _listenSocket = null;

                    return false;
                }
            }

#if Matrix_Diagnostics
            Monitor.Info("Message server started, at [" + endPoint.ToString() + "].");
#endif

            AssignAsyncAcceptArgs();
            return true;
        }

        /// <summary>
        /// Stop the server.
        /// </summary>
        public void Stop(TimeSpan? timeOut)
        {
            System.Net.Sockets.Socket listenSocket;
            lock (_syncRoot)
            {
                listenSocket = _listenSocket;
                _listenSocket = null;
            }

            if (listenSocket != null)
            {
                if (timeOut.HasValue)
                {
                    listenSocket.Close((int)timeOut.Value.TotalMilliseconds);
                }
                else
                {
                    listenSocket.Close();
                }
            }
        }

        public bool DisconnectClient(int clientId)
        {
            SocketCommunicatorEx client;
            if (_clientsHotSwap.TryGetValue(clientId, out client) == false)
            {
                return false;
            }

            return client.DisconnectAsync();
        }


        /// <summary>
        /// Send to all.
        /// </summary>
        public void SendAsync(object message, TimeSpan? requestConfirmTimeout)
        {
            foreach (SocketCommunicatorEx client in _clientsHotSwap.Values)
            {
                client.SendAsync(message, requestConfirmTimeout);
            }
        }

        /// <summary>
        /// Send a message to a client.
        /// </summary>
        /// <param name="clientId">Id of the client.</param>
        /// <param name="message">Message to send.</param>
        /// <returns>True if send has started successfully.</returns>
        public bool SendAsync(int clientId, object message, TimeSpan? requestConfirmTimeout)
        {
            SocketCommunicatorEx client;
            if (_clientsHotSwap.TryGetValue(clientId, out client) == false)
            {
#if Matrix_Diagnostics
                Monitor.OperationError("Client [" + clientId + "] not found.");
#endif
                return false;
            }

            return client.SendAsync(message, requestConfirmTimeout) != SocketCommunicator.InvalidSendIndex;
        }

        /// <summary>
        /// Helper, assign the pending async accept args.
        /// </summary>
        SocketAsyncEventArgs AssignAsyncAcceptArgs()
        {
            System.Net.Sockets.Socket listenSocket = _listenSocket;
            if (listenSocket == null)
            {
                return null;
            }

            SocketAsyncEventArgs e = new SocketAsyncEventArgs();
            e.Completed += new EventHandler<SocketAsyncEventArgs>(SocketAsyncEventArgs_Completed);

            if (listenSocket.AcceptAsync(e) == false)
            {
                if (e.SocketError == SocketError.Success)
                {
                    SocketAsyncEventArgs_Completed(this, e);
                }
                else
                {// Accept failed.
#if Matrix_Diagnostics
                    Monitor.Fatal("Async accept failed.");
#endif
                }
            }

            return e;
        }

        /// <summary>
        /// Client connected.
        /// </summary>
        private void SocketAsyncEventArgs_Completed(object sender, SocketAsyncEventArgs e)
        {
            try
            {
                if (e.SocketError != SocketError.Success)
                {// This will execute the finally block, but skip the AssignAsyncAcceptArgs().
                    return;
                }

                if (e.LastOperation == SocketAsyncOperation.Accept
                    && e.SocketError == SocketError.Success)
                {
                    SocketCommunicatorEx helper = new SocketCommunicatorEx(_serializer);
                    helper.AssignSocket(e.AcceptSocket, true);
                    helper.Id = PendingClientId;

#if Matrix_Diagnostics
                    helper.Monitor.MinimumTracePriority = Monitor.MinimumTracePriority;
#endif

                    helper.ConnectedEvent += new SocketCommunicator.HelperUpdateDelegate(helper_ConnectedEvent);
                    helper.DisconnectedEvent += new SocketCommunicator.HelperUpdateDelegate(helper_DisconnectedEvent);

                    helper.MessageReceivedEvent += new SocketCommunicator.MessageUpdateDelegate(helper_MessageReceivedEvent);
                    helper.SendAsyncCompleteEvent += new SocketCommunicator.AsyncMessageSendDelegate(helper_SendAsyncCompleteEvent);

                    _clientsHotSwap[(int)helper.Id] = helper;

#if Matrix_Diagnostics
                    Monitor.ReportImportant("Client [" + helper.Id + "] connected.");
#endif

                    ServerClientUpdateDelegate delegateInstance = ClientConnectedEvent;
                    if (delegateInstance != null)
                    {
                        delegateInstance(this, helper);
                    }
                }
                else
                {
#if Matrix_Diagnostics
                    Monitor.NotImplementedWarning(e.ToString());
#endif
                }

            }
            finally
            {
                e.Completed -= new EventHandler<SocketAsyncEventArgs>(SocketAsyncEventArgs_Completed);
                e.Dispose();
            }

            AssignAsyncAcceptArgs();
        }

        #region Helper Instances Events

        void helper_SendAsyncCompleteEvent(SocketCommunicator helper, SocketCommunicator.AsyncMessageSendInfo info)
        {
            AsyncMessageSendUpdateDelegate del = ClientAsyncMessageSendEvent;
            if (del != null)
            {
                del(this, helper as SocketCommunicatorEx, info);
            }
        }

        void helper_MessageReceivedEvent(SocketCommunicator helper, object message)
        {
            MessageUpdateDelegate del = ClientMessageReceivedEvent;
            if (del != null)
            {
                del(this, (SocketCommunicatorEx)helper, message);
            }
        }

        void helper_ConnectedEvent(SocketCommunicator helper)
        {
            // This is never invoked, since we create the helpers directly on the connected sockets.
        }

        void helper_DisconnectedEvent(SocketCommunicator client)
        {
            //Monitor.ReportImportant("Client [" + client.Id + "] disconnected.");

            ServerClientUpdateDelegate delegateInstance = ClientDisconnectedEvent;
            if (delegateInstance != null)
            {
                delegateInstance(this, client as SocketCommunicatorEx);
            }
        }

        #endregion

    }
}
