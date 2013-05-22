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
using System.Net;
using System.Threading;
using Matrix.Common.Core.Serialization;

namespace Matrix.Common.Sockets.Common
{
    /// <summary>
    /// Extends the communicator class with active connection capabilities.
    /// </summary>
    public class SocketClientCommunicator : SocketCommunicatorEx
    {
        EndPoint _endPoint = null;

        SocketAsyncEventArgs _asyncConnectArgs = null;
        
        /// <summary>
        /// 
        /// </summary>
        public override EndPoint EndPoint
        {
            get
            {
                return _endPoint;
            }
        }

        Timer _autoConnectTimer;

        bool _autoReconnect = false;
        /// <summary>
        /// Is the client trying to auto reconnect.
        /// </summary>
        public bool AutoReconnect
        {
            get { return _autoReconnect; }
            
            set 
            {
                if (_autoReconnect == value)
                {
                    return;
                }

                _autoReconnect = value;
                
                lock (_syncRoot)
                {
                    if (value == false && _autoConnectTimer != null)
                    {// Release the current timer.
                        _autoConnectTimer.Dispose();
                        _autoConnectTimer = null;
                    }
                    else if (value && _autoConnectTimer == null)
                    {// Create new timer.
                        _autoConnectTimer = new Timer(AutoConnectTimerCallbackMethod, null,
                                                      TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5));
                    }
                }
            }
        }


        /// <summary>
        /// Constructor.
        /// </summary>
        public SocketClientCommunicator(ISerializer serializer)
            : base(serializer)
        {
            AutoReconnect = false;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public SocketClientCommunicator(EndPoint endPoint, ISerializer serializer)
            : base(serializer)
        {
            _endPoint = endPoint;
            AutoReconnect = false;
        }

        public override void Dispose()
        {
            // Stop the timer, in case it is running.
            AutoReconnect = false;

            base.Dispose();
        }

        protected void AutoConnectTimerCallbackMethod(object state)
        {
            if (_autoReconnect == false)
            {
                lock (_syncRoot)
                {
                    if (_autoConnectTimer != null)
                    {
                        _autoConnectTimer.Dispose();
                        _autoConnectTimer = null;
                    }
                }
            }

            if (_endPoint != null && _autoReconnect && IsConnected == false)
            {
                ConnectAsync(_endPoint);
            }
        }

        /// <summary>
        /// Begin asynchronous connect.
        /// </summary>
        /// <param name="endPoint"></param>
        /// <returns></returns>
        public bool ConnectAsync(EndPoint endPoint)
        {
            lock (_syncRoot)
            {
                if (_asyncConnectArgs != null)
                {// Connection in progress.
                    return true;
                }
            }

            System.Net.Sockets.Socket socket = _socket;
            if (IsConnected)
            {// Already connected.
                if (socket != null)
                {
                    try
                    {
                        if (socket.RemoteEndPoint.Equals(endPoint))
                        {// Connected to given endPoint.
                            return true;
                        }
                        else
                        {// Connected to some other endPoint
                            ReleaseSocket(true);
                        }
                    }
                    catch (Exception ex)
                    {// socket.RemoteEndPoint can throw.
                        ReleaseSocket(true);
#if Matrix_Diagnostics
                        Monitor.OperationError("Failed to start async connect", ex);
#endif
                        return false;
                    }
                }
            }

            SocketAsyncEventArgs args;
            lock(_syncRoot)
            {
                if (_asyncConnectArgs != null)
                {// Connection in progress.
                    return true;
                }

                _socket = new System.Net.Sockets.Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                socket = _socket;

                _asyncConnectArgs = new SocketAsyncEventArgs();
                _asyncConnectArgs.Completed += new EventHandler<SocketAsyncEventArgs>(SocketAsyncEventArgs_Connected);
                _asyncConnectArgs.RemoteEndPoint = endPoint;
                _endPoint = endPoint;
                args = _asyncConnectArgs;
            }

            if (socket != null && socket.ConnectAsync(args) == false)
            {
                SocketAsyncEventArgs_Connected(this, args);
            }

            return true;
        }

        void SocketAsyncEventArgs_Connected(object sender, SocketAsyncEventArgs e)
        {
            if (e.LastOperation == SocketAsyncOperation.Connect)
            {
                if (e.SocketError == SocketError.Success)
                {
#if Matrix_Diagnostics
                    Monitor.ReportImportant("Socket connected.");
#endif
                    RaiseConnectedEvent();
                    AssignAsyncReceiveArgs(false);
                }
                else if (e.SocketError == SocketError.IsConnected)
                {// Already connected.
                    // Connect failed.
#if Matrix_Diagnostics
                    Monitor.ReportImportant("Socket already connected.");
#endif
                }
                else
                {
#if Matrix_Diagnostics
                    Monitor.ReportImportant("Socket connection failed: " + e.SocketError.ToString());
#endif
                }
            }
            else
            {
                // Connect failed.
#if Matrix_Diagnostics
                Monitor.ReportImportant("Socket async connect failed.");
#endif
            }

            lock (_syncRoot)
            {
                if (_asyncConnectArgs == e)
                {
                    _asyncConnectArgs.Dispose();
                    _asyncConnectArgs = null;
                }
                else
                {
#if Matrix_Diagnostics
                    Monitor.Error("SocketAsyncEventArgs mismatch.");
#endif
                    e.Dispose();
                    _asyncConnectArgs = null;
                }
            }

        }

    }
}
