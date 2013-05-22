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
using System.Text;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using System.Net;
using Matrix.Common.Core.Serialization;
using Matrix.Common.Core;

#if Matrix_Diagnostics
    using Matrix.Common.Diagnostics;
#endif

namespace Matrix.Common.Sockets.Common
{
    /// <summary>
    /// Class wraps the complexities of working with a async socket.
    /// </summary>
    public class SocketCommunicator : IDisposable
    {
        public const int InvalidSendIndex = -1;

        protected object _syncRoot = new object();

        int _id = -1;
        /// <summary>
        /// Applied in the server client connections management.
        /// </summary>
        public int Id
        {
            get { return _id; }
            set { _id = value; }
        }

        /// <summary>
        /// Establish the end point routine. Default behaviour serves server and client mode,
        /// but will only report properly when connected.
        /// </summary>
        public virtual EndPoint EndPoint
        {
            get 
            {
                System.Net.Sockets.Socket socket = _socket;
                if (socket != null && socket.Connected)
                {
                    try
                    {
                        return socket.RemoteEndPoint;
                    }
                    catch (Exception ex)
                    {
#if Matrix_Diagnostics
                        Monitor.OperationError("Failed to establish end point", ex);
#endif
                    }
                }

                return null;
            }
        }

        long _pendingSendId = 0;
        public long PendingSendId
        {
            get { return Interlocked.Increment(ref _pendingSendId); }
        }

#if Matrix_Diagnostics
        readonly InstanceMonitor _monitor;
        /// <summary>
        /// 
        /// </summary>
        public InstanceMonitor Monitor
        {
            get { return _monitor; }
        }
#endif

        ISerializer _serializer;

        /// <summary>
        /// Receive buffers.
        /// </summary>
        byte[] _receiveBuffer = new byte[32 * 1024];

        protected System.Net.Sockets.Socket _socket = null;
        
        public bool IsConnected
        {
            get
            {
                System.Net.Sockets.Socket socket = _socket;
                if (socket != null)
                {
                    return socket.Connected;
                }

                return false;
            }
        }

        /// <summary>
        /// We use these 2 streams and constantly swap between them, in order to reuse memory
        /// and not recreate a mem stream each time a buffer comes in.
        /// </summary>
        MemoryStream _pendingReceiveStream = new MemoryStream();
        MemoryStream _pendingReceiveStreamSecondary = new MemoryStream();

        /// <summary>
        /// We use this to evade any possible scenario, where 2 of those get assigned at the same
        /// time (since that causes total destruction, and buffer mixups).
        /// </summary>
        SocketAsyncEventArgs _lastReceiveArgs = null;

        /// <summary>
        /// Class stores information on sending a message.
        /// </summary>
        public class AsyncMessageSendInfo : EventArgs, IDisposable
        {
            public long Id { get; set; }
            public System.Net.Sockets.Socket Socket { get; set; }
            public object UserToken { get; set; }
            public object Message { get; set; }
            public MemoryStream Stream { get; set; }

            public ManualResetEvent ConfirmationEvent { get; set; }

            public void Dispose()
            {
                Message = null;
                UserToken = null;
                Socket = null;

                ManualResetEvent confirmationEvent = ConfirmationEvent;
                ConfirmationEvent = null;
                if (confirmationEvent != null)
                {
                    try
                    {
                        confirmationEvent.Close();
                    }
                    catch (Exception ex)
                    {
#if Matrix_Diagnostics
                        SystemMonitor.Error("Failed to close event", ex);
#endif
                    }
                }

                MemoryStream stream = Stream;
                if (stream != null)
                {
                    stream.Dispose();
                }
                Stream = null;
            }
        }

        public delegate void MessageUpdateDelegate(SocketCommunicator helper, object message);
        public delegate void HelperUpdateDelegate(SocketCommunicator helper);
        public delegate void AsyncMessageSendDelegate(SocketCommunicator helper, AsyncMessageSendInfo info);

        public event AsyncMessageSendDelegate SendAsyncCompleteEvent;
        public event MessageUpdateDelegate MessageReceivedEvent;

        public event HelperUpdateDelegate ConnectedEvent;
        public event HelperUpdateDelegate DisconnectedEvent;

        /// <summary>
        /// Constructor, when socket already connected.
        /// </summary>
        /// <param name="messageSerializer"></param>
        /// <param name="socket"></param>
        public SocketCommunicator(ISerializer serializer)
        {
#if Matrix_Diagnostics
            _monitor = new InstanceMonitor(this);
            Monitor.Info("Socket communicator created.");
#endif

            _serializer = serializer;
        }

        public virtual void Dispose()
        {
#if Matrix_Diagnostics
            Monitor.Info("Socket communicator diposed."); 
#endif
            System.Net.Sockets.Socket socket = _socket;
            _socket = null;
            if (socket != null)
            {
                socket.Close();
            }

            SendAsyncCompleteEvent = null;
            MessageReceivedEvent = null;

            ConnectedEvent = null;
            DisconnectedEvent = null;

            ReleaseAsyncReceiveArgs();

#if Matrix_Diagnostics
            _monitor.Dispose();
#endif
            _serializer = null;
        }

        public override string ToString()
        {
            System.Net.Sockets.Socket socket = _socket;
            if (socket != null)
            {
                EndPoint endPoint = EndPoint;
                if (endPoint != null)
                {
                    return string.Format("{0}, id [{1}], at {2}", this.GetType().Name, Id, endPoint.ToString());
                }
            }

            return string.Format("{0}, id [{1}], at NA", this.GetType().Name, Id);
        }

        /// <summary>
        /// Attach object to a new socket.
        /// </summary>
        /// <param name="socket"></param>
        /// <returns></returns>
        public void AssignSocket(System.Net.Sockets.Socket socket, bool disposeCurrentSocket)
        {
#if Matrix_Diagnostics
            InstanceMonitor monitor = Monitor;
            if (monitor != null && monitor.IsReportAllowed)
            {
                monitor.Info(string.Format("Socket assigned, disposing current [{0}].", disposeCurrentSocket));
            }
#endif

            ReleaseSocket(disposeCurrentSocket);
            lock (_syncRoot)
            {
                _socket = socket;
            }

            AssignAsyncReceiveArgs(true);
        }

        /// <summary>
        /// Release the currently used socket.
        /// </summary>
        public void ReleaseSocket(bool disposeSocket)
        {
#if Matrix_Diagnostics
            InstanceMonitor monitor = Monitor;
            if (monitor != null && monitor.IsReportAllowed)
            {
                monitor.Info(string.Format("Releasing socket, dipose [{0}].", disposeSocket));
            }
#endif

            lock (_syncRoot)
            {
                System.Net.Sockets.Socket socket = _socket;
                if (socket != null && disposeSocket)
                {
                    socket.Disconnect(false);
                    socket.Close();
                }
                _socket = null;
            }
        }

        protected void RaiseConnectedEvent()
        {
#if Matrix_Diagnostics
            InstanceMonitor monitor = Monitor;
            if (monitor != null && monitor.IsReportAllowed)
            {
                monitor.Info("Raising connected event.");
            }
#endif

            SocketCommunicator.HelperUpdateDelegate delegateInstance = ConnectedEvent;
            if (delegateInstance != null)
            {
                delegateInstance(this);
            }
        }

        protected void RaiseDisconnectedEvent()
        {
#if Matrix_Diagnostics
            InstanceMonitor monitor = Monitor;
            if (monitor != null && monitor.IsReportAllowed)
            {
                monitor.Info("Raising disconnected event.");
            }
#endif
            
            SocketCommunicator.HelperUpdateDelegate delegateInstance = DisconnectedEvent;
            if (delegateInstance != null)
            {
                delegateInstance(this);
            }
        }

        /// <summary>
        /// Helper, assign a new set of receive args.
        /// </summary>
        protected bool AssignAsyncReceiveArgs(bool releaseExisting)
        {
#if Matrix_Diagnostics
            InstanceMonitor monitor = Monitor;
            if (monitor != null && monitor.IsReportAllowed)
            {
                monitor.Info(string.Format("AssignAsyncReceiveArgs, release existing [{0}].", releaseExisting));
            }
#endif

            System.Net.Sockets.Socket socket = _socket;
            if (socket == null)
            {
                return false;
            }

            if (releaseExisting)
            {
                ReleaseAsyncReceiveArgs();
            }

            SocketAsyncEventArgs args;
            lock (_syncRoot)
            {
                if (_lastReceiveArgs != null)
                {
                    _lastReceiveArgs.Dispose();
                    _lastReceiveArgs = null;
#if Matrix_Diagnostics
                    Monitor.Fatal("Assign async receive args logic error.");
#endif
                    return false;
                }

                _lastReceiveArgs = new SocketAsyncEventArgs();
                _lastReceiveArgs.Completed += new EventHandler<SocketAsyncEventArgs>(SocketAsyncEventArgs_Received);

                _lastReceiveArgs.SetBuffer(_receiveBuffer, 0, _receiveBuffer.Length);
                args = _lastReceiveArgs;
            }

            if (socket.ReceiveAsync(args) == false)
            {
                if (args.SocketError == SocketError.Success)
                {
                    SocketAsyncEventArgs_Received(socket, args);
                }
                else
                {
                    ReleaseAsyncReceiveArgs();
                    return false;
                }
            }

            return true;    
        }


        void SocketAsyncEventArgs_Received(object sender, SocketAsyncEventArgs e)
        {

#if Matrix_Diagnostics
            if (Monitor.IsReportAllowed)
            {
                Monitor.Info("Socket received: " + e.LastOperation.ToString() + " was " + e.SocketError + " data [" + e.BytesTransferred + "]");
            }
#endif

//            // Do nothing here, since all other places for this cause various problems!
//            if (e.BytesTransferred == 0)
//            {
//#if Matrix_Diagnostics
//                InstanceMonitor monitor = Monitor;
//                if (monitor != null && monitor.IsReportAllowed)
//                {
//                    monitor.Info("Zero bytes transferred.");
//                }
//#endif
//                return;
//            }

            bool resetReceiveArgs = true;
            try
            {
                if (e != _lastReceiveArgs)
                {// We must make sure that we only handle these one at a time.
#if Matrix_Diagnostics
                    Monitor.Fatal("Operation logic error in receive args.");
#endif

                    e.Completed -= new EventHandler<SocketAsyncEventArgs>(SocketAsyncEventArgs_Received);
                    e.Dispose();

                    resetReceiveArgs = false;
                    return;
                }

                if (e.SocketError == SocketError.ConnectionReset
                    /* TODO: THIS WAS ACTIVE, BUT WAS REMOVED SINCE IT CAUSED A PROBLEM IN VALID CONDITIONS
                     * TEST TO SEE IF LINE STILL NEEDED
                     * SO NOW THIS IS MOVED DOWNWARDS.
                     * || e.BytesTransferred == 0*/
                    )
                {// Connection was reset.
#if Matrix_Diagnostics
                    Monitor.ReportImportant("Socket connection reset.");
#endif
                    RaiseDisconnectedEvent();
                    resetReceiveArgs = false;
                    return;
                }

                System.Net.Sockets.Socket socket = _socket;
                if (socket == null)
                {
                    resetReceiveArgs = false;
                    return;
                }

                if (e.SocketError != SocketError.Success || socket.Connected == false)
                {
                    resetReceiveArgs = false;
                    return;
                }

                if (e.BytesTransferred == 0)
                {
                    resetReceiveArgs = false;
                    return;
                }

                lock (_syncRoot)
                {// Start the stream operations.

                    long streamStartPosition = _pendingReceiveStream.Position;

                    _pendingReceiveStream.Seek(0, SeekOrigin.End);
                    _pendingReceiveStream.Write(e.Buffer, 0, e.BytesTransferred);

                    _pendingReceiveStream.Seek(streamStartPosition, SeekOrigin.Begin);
                }

                ISerializer serializer = _serializer;
                if (serializer == null)
                {
                    return;
                }

                object message = null;
                do
                {
                    lock (_syncRoot)
                    {
                        if (_pendingReceiveStream.Length <= _pendingReceiveStream.Position)
                        {// Already read to the end of stream.
                            break;
                        }

                        long startPosition = _pendingReceiveStream.Position;
                        try
                        {
                            message = serializer.Deserialize(_pendingReceiveStream);
                            if (message == null && _pendingReceiveStream.Position != startPosition)
                            {// No message was retrieved, and stream was corrupted.
                                throw new InvalidDataException();
                            }
                        }
                        catch (InvalidDataException ex)
                        {   
                            // The serialization routine has failed, or the stream is corrupt;
                            // clear everything and try to start over (error recovery).
                            message = null;
                            _pendingReceiveStream.SetLength(0);
#if Matrix_Diagnostics
                            Monitor.Error("Possible invalid position calculation, data lost.", ex);
#endif
                        }
                    }

                    if (message != null)
                    {
                        if (message is SystemMessage)
                        {// System message received.
                            ProcessSystemMessage(message as SystemMessage);
                        }
                        else
                        {// Custom user message.
                            MessageUpdateDelegate delegateInstance = MessageReceivedEvent;
                            if (delegateInstance != null)
                            {
                                delegateInstance(this, message);
                            }
                        }
                    }


                } 
                while (message != null);


                lock (_syncRoot)
                {
                    if (_pendingReceiveStream.Position == _pendingReceiveStream.Length)
                    {// Reset the receive stream.
                        _pendingReceiveStream.SetLength(0);
                    }
                    else
                    {
                        // Swap primary and secondary streams, copying over the remaining data.
                        MemoryStream existingStream = _pendingReceiveStream;
                        MemoryStream newStream = _pendingReceiveStreamSecondary;

                        newStream.SetLength(existingStream.Length - existingStream.Position);
                        // Copy the left over data.
                        existingStream.Read(newStream.GetBuffer(), 0, (int)newStream.Length);
                        newStream.Seek(0, SeekOrigin.Begin);

                        CommonHelper.Swap<MemoryStream>(ref _pendingReceiveStream, ref _pendingReceiveStreamSecondary);
                        _pendingReceiveStreamSecondary.SetLength(0);

                        if (existingStream.Position != existingStream.Length)
                        {
#if Matrix_Diagnostics
                            Monitor.Error("Data propagation error.");
#endif
                            throw new SystemException("Data propagation error.");
                        }
                    }
                }

            }
            finally
            {
                // This is crucial, otherwise there is memory leaks.
                //e.Dispose();

                if (resetReceiveArgs)
                {
                    AssignAsyncReceiveArgs(true);
                }
                else
                {
                    ReleaseAsyncReceiveArgs();
                }
            }
        }

        /// <summary>
        /// Allows to process system messages in a different way, should there be one.
        /// </summary>
        /// <param name="message"></param>
        internal virtual void ProcessSystemMessage(SystemMessage message)
        {
        }

        /// <summary>
        /// Begin an asynchronous disconnect.
        /// </summary>
        /// <returns></returns>
        public bool DisconnectAsync()
        {

#if Matrix_Diagnostics
            InstanceMonitor monitor = Monitor;
            if (monitor != null && monitor.IsReportAllowed)
            {
                monitor.Info("Async disconnecting started.");
            }
#endif

            SocketAsyncEventArgs asyncDisconnectArgs = new SocketAsyncEventArgs();
            asyncDisconnectArgs.Completed += new EventHandler<SocketAsyncEventArgs>(SocketAsyncEventArgs_DisconnectComplete);

            if (_socket.DisconnectAsync(asyncDisconnectArgs) == false)
            {

#if Matrix_Diagnostics
                monitor = Monitor;
                if (monitor != null && monitor.IsReportAllowed)
                {
                    monitor.Info("Disconnecting already completed.");
                }
#endif

                SocketAsyncEventArgs_DisconnectComplete(this, asyncDisconnectArgs);
            }

            return true;
        }

        /// <summary>
        /// Disconnect was completed.
        /// </summary>
        void SocketAsyncEventArgs_DisconnectComplete(object sender, SocketAsyncEventArgs e)
        {
#if Matrix_Diagnostics
            InstanceMonitor monitor = Monitor;
            if (monitor != null && monitor.IsReportAllowed)
            {
                monitor.Info("Disconnecting completed.");
            }
#endif

            if (e.SocketError == SocketError.Success)
            {
                e.Completed -= new EventHandler<SocketAsyncEventArgs>(SocketAsyncEventArgs_DisconnectComplete);
                e.Dispose();

#if Matrix_Diagnostics
                Monitor.ReportImportant("Disconnected.");
#endif
                RaiseDisconnectedEvent();
            }

            ReleaseAsyncReceiveArgs();
        }

        void ReleaseAsyncReceiveArgs()
        {
#if Matrix_Diagnostics
            InstanceMonitor monitor = Monitor;
            if (monitor != null && monitor.IsReportAllowed)
            {
                monitor.Info("Releasing async receive arguments.");
            }
#endif

            lock (_syncRoot)
            {
                if (_lastReceiveArgs != null)
                {// Release the existing receive args.
                    _lastReceiveArgs.Completed -= new EventHandler<SocketAsyncEventArgs>(SocketAsyncEventArgs_Received);
                    _lastReceiveArgs.Dispose();
                    _lastReceiveArgs = null;
                }
            }
        }

        /// <summary>
        /// Send a message, asynchronously.
        /// </summary>
        /// <param name="message">The message being sent.</param>
        /// <param name="requestConfirm">Should we wait for a confirmation, the </param>
        /// <returns>The id of the send message, or negative value (InvalidSendIndex / -1) if send failed.</returns>
        public long SendAsync(object message, TimeSpan? requestConfirmTimeout)
        {
#if Matrix_Diagnostics
            InstanceMonitor monitor = Monitor;
            if (monitor != null && monitor.IsReportAllowed)
            {
                monitor.Info(string.Format("Async sending message [{0}].", message.ToString()));
            }
#endif

            System.Net.Sockets.Socket socket = _socket;
            if (IsConnected == false || socket == null)
            {
#if Matrix_Diagnostics
                Monitor.OperationError("Communicator can not send message [" + message.ToString() + "] since not connected.");
#endif
                return InvalidSendIndex;
            }

            ISerializer serializer = _serializer;
            if (serializer == null)
            {
                return InvalidSendIndex;
            }

            // Event used for confirmed calls
            ManualResetEvent sendCompleteEvent = null;
            if (requestConfirmTimeout.HasValue)
            {
                sendCompleteEvent = new ManualResetEvent(false);
            }

            AsyncMessageSendInfo messageSendInfo = new AsyncMessageSendInfo() {
                                                                                  Id = PendingSendId,
                                                                                  Socket = socket,
                                                                                  Message = message,
                                                                                  ConfirmationEvent = sendCompleteEvent,
                                                                              };

            SocketAsyncEventArgs e = new SocketAsyncEventArgs();
            e.UserToken = messageSendInfo;
            e.Completed += new EventHandler<SocketAsyncEventArgs>(SocketAsyncEventArgs_SendComplete);
            
            messageSendInfo.Stream = new MemoryStream();
            if (serializer.Serialize(messageSendInfo.Stream, message) == false)
            {
                messageSendInfo.Dispose();
                return InvalidSendIndex;
            }

            e.SetBuffer(messageSendInfo.Stream.GetBuffer(), 0, (int)messageSendInfo.Stream.Length);
            if (messageSendInfo.Socket.SendAsync(e) == false)
            {
                messageSendInfo.Dispose();
            }

            // Reaquire the event, to lessen the chance of [ObjectDisposedException]
            // when the connection is not established and we get errors on complete instantly.
            sendCompleteEvent = messageSendInfo.ConfirmationEvent;
            if (sendCompleteEvent != null)
            {
                try
                {
                    if (sendCompleteEvent.WaitOne(requestConfirmTimeout.Value) == false)
                    {
#if Matrix_Diagnostics
                        Monitor.OperationError("Communicator could not confirm message sent in assigned timeout.");
#endif
                        return InvalidSendIndex;
                    }
                }
                catch (ObjectDisposedException)
                {
#if Matrix_Diagnostics
                    Monitor.OperationError("Communicator could not confirm message sent in assigned timeout, due to event disposed.");
#endif
                    return InvalidSendIndex;
                }
            }

            return messageSendInfo.Id;
        }

        private void SocketAsyncEventArgs_SendComplete(object sender, SocketAsyncEventArgs e)
        {
#if Matrix_Diagnostics
            InstanceMonitor monitor = Monitor;
            if (monitor != null && monitor.IsReportAllowed)
            {
                monitor.Info(string.Format("Send complete, sender [{0}].", sender != null ? sender.ToString() : string.Empty));
            }
#endif

            e.Completed -= new EventHandler<SocketAsyncEventArgs>(SocketAsyncEventArgs_SendComplete);
            
            AsyncMessageSendInfo sendInfo = (AsyncMessageSendInfo)e.UserToken;
            if (sendInfo.ConfirmationEvent != null)
            {// Signal any waiter, we sent the message.
                sendInfo.ConfirmationEvent.Set();
            }

            e.Dispose();

#if Matrix_Diagnostics
            if (Monitor.IsReportAllowed)
            {
                Monitor.Info(this.ToString() + " message send complete [" + sendInfo.Message.ToString() + "]");
            }
#endif

            AsyncMessageSendDelegate delegateInstance = SendAsyncCompleteEvent;
            if (delegateInstance != null)
            {
                delegateInstance(this, sendInfo);
            }
        }

    }
}
