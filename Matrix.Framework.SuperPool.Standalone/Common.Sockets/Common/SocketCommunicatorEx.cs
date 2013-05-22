// -----
// Copyright 2010 Deyan Timnev
// This file is part of the Matrix Platform (www.matrixplatform.com).
// The Matrix Platform is free software: you can redistribute it and/or modify it under the terms of the GNU Lesser General Public License as published by the Free Software Foundation, 
// either version 3 of the License, or (at your option) any later version. The Matrix Platform is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; 
// without even the implied warranty of  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Lesser General Public License for more details.
// You should have received a copy of the GNU Lesser General Public License along with the Matrix Platform. If not, see http://www.gnu.org/licenses/lgpl.html
// -----
using System;
using System.Threading;
using Matrix.Common.Core.Serialization;

#if Matrix_Diagnostics
    using Matrix.Common.Diagnostics;
#endif

namespace Matrix.Common.Sockets.Common
{
    /// <summary>
    /// Extends the communicator class by adding keep alive functionality.
    /// </summary>
    public class SocketCommunicatorEx : SocketCommunicator
    {
        volatile bool _keepAlive = false;

        /// <summary>
        /// Keep alive will send, each few seconds, a ping and wait for a ping back;
        /// in case nothing comes in will declare the connection dead.
        /// </summary>
        public bool KeepAlive
        {
            get { return _keepAlive; }
            set 
            { 
                _keepAlive = value;
                
                if (value)
                {
                    ConstructTimer();
                }
                else
                {
                    ReleaseTimer();
                }
            }
        }

        /// <summary>
        /// Timer controls auto reconnection.
        /// </summary>
        Timer _timer = null;
        
        TimeSpan _keepAliveTimerInterval = TimeSpan.FromSeconds(45);
        public TimeSpan KeepAliveTimerInterval
        {
            get { return _keepAliveTimerInterval; }
            set 
            { 
                _keepAliveTimerInterval = value;
                Timer timer = _timer;
                if (timer != null)
                {
                    timer.Change((int)TimeSpan.FromSeconds(1.5).TotalMilliseconds,
                        (int)KeepAliveTimerInterval.TotalMilliseconds);
                }
            }
        }

        TimeSpan _keepAliveTimeoutInterval = TimeSpan.FromSeconds(160);
        public TimeSpan KeepAliveTimeoutInterval
        {
            get { return _keepAliveTimeoutInterval; }
            set { _keepAliveTimeoutInterval = value; }
        }
        
        DateTime _lastMessageReceivedTime = DateTime.Now;

        /// <summary>
        /// Constructor.
        /// </summary>
        public SocketCommunicatorEx(ISerializer serializer)
            : base(serializer)
        {
            KeepAlive = false;
        }

        public override void Dispose()
        {
            ReleaseTimer();
            base.Dispose();
        }

        protected void ConstructTimer()
        {
            ReleaseTimer();
            lock (_syncRoot)
            {
                if (_timer != null)
                {
#if Matrix_Diagnostics
                    Monitor.Error("Timer already constructed.");
#endif
                    return;
                }

                _timer = new Timer(TimerCallbackMethod, null,
                                   TimeSpan.FromSeconds(1.5), KeepAliveTimerInterval);
            }
        }

        protected void ReleaseTimer()
        {
            Timer timer = _timer;
            _timer = null;
            if (timer != null)
            {
                timer.Dispose();
            }
        }

        internal override void ProcessSystemMessage(SystemMessage message)
        {// Received a system message.

#if Matrix_Diagnostics
            InstanceMonitor monitor = Monitor;
            if (monitor != null && monitor.IsReportAllowed)
            {
                monitor.Info(string.Format("Processing system message [{0}].", message.ToString()));
            }
#endif

            if (message.Type == SystemMessage.TypeEnum.KeepAlive
                && this.KeepAlive == false)
            {// Make sure to activate keep alive on this side too.
#if Matrix_Diagnostics
                monitor.Warning("Received KeepAlive, and KeepAlive not active on this client, auto-activating.");
#endif
                this.KeepAlive = true;
            }

            _lastMessageReceivedTime = DateTime.Now;
        }

        /// <summary>
        /// TODO: Possible bug: behavior observed, the system "fell asleep" for 4 minutes
        /// and when it woke up, the timer started 5 threads at the same time on this method.
        /// 
        /// They all raised disconnected events, but the remaining of the system seemed to be OK ?!
        /// 
        /// This happened only once on a Windows machine that has been running with no restart 
        /// for a few weeks.
        /// </summary>
        /// <param name="state"></param>
        private void TimerCallbackMethod(object state)
        {
            if (_keepAlive == false)
            {
                ReleaseTimer();
                return;
            }

            if (IsConnected)
            {
                // Send an empty system message to keep alive.
                SendAsync(new SystemMessage(), null);
            }

            if (IsConnected && DateTime.Now - _lastMessageReceivedTime > KeepAliveTimeoutInterval)
            {// Signal disconnection.
#if Matrix_Diagnostics
                InstanceMonitor monitor = Monitor;
                if (monitor != null && monitor.IsReportAllowed)
                {
                    monitor.Info("Disconnecting due to no activity timeout.");
                }
#endif
             
                DisconnectAsync();
            }
        }


    }
}
