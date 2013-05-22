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
using Matrix.Framework.SuperPool.Clients;

#if Matrix_Diagnostics
    using Matrix.Common.Diagnostics;
#endif

namespace Matrix.Framework.SuperPool.Call
{
    /// <summary>
    /// Stores information on synchronous (or asynchronous but waiting for a result) super pool calls.
    /// </summary>
    public class SyncCallInfo : IDisposable
    {
        #region Common

        public DateTime CreationTime { get; protected set; }
        public long CallId { get; protected set; }

        #endregion

        public object Response { get; set; }
        public SuperPoolCall RequestCall { get; set; }
        public ManualResetEvent Event { get; set; }
        public Exception Exception { get; protected set; }

        #region Async Result Waiting Call

        public AsyncCallResultDelegate AsyncResultDelegate { get; set; }
        public object AsyncResultState { get; set; }
        /// <summary>
        /// Used when a call was made to any implementor, and results are collection in 
        /// async fashion, with this max. timeout allowed for a result.
        /// </summary>
        public TimeSpan? AsyncResultTimeout { get; set; }

        #endregion

        /// <summary>
        /// A multi response call is waiting for multiple results to come in.
        /// </summary>
        public bool IsMultiResponse
        {
            get
            {
                return (AsyncResultDelegate != null && AsyncResultTimeout.HasValue);
            }
        }

        public bool IsMultiResponseComplete
        {
            get
            {
                return (AsyncResultDelegate != null && AsyncResultTimeout.HasValue
                        && (DateTime.Now - CreationTime) > AsyncResultTimeout.Value);
            }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public SyncCallInfo(long callId)
        {
            this.CallId = callId;
            CreationTime = DateTime.Now;
        }

        public void AcceptResponse(SuperPoolClient client, object result, Exception exception)
        {
            Response = result;

            if (AsyncResultDelegate != null)
            {
                try
                {
                    AsyncResultDelegate.Invoke(client, new AsyncResultParams() { Result = result, State = AsyncResultState, Exception = exception });
                }
                catch (Exception ex)
                {
#if Matrix_Diagnostics
                    SystemMonitor.OperationError(string.Format("AcceptReposnse invoke of client [{0}] has caused an exception", client.Name), ex);
#endif
                }
            }
            else
            {// Assign the parameter only in sync calls, since async already consumed it trough async delegate.
                Exception = exception;
            }

            ManualResetEvent eventInstance = Event;
            if (eventInstance != null)
            {
                eventInstance.Set();
            }
        }

        #region IDisposable Members

        public void Dispose()
        {
            Response = null;
            
            ManualResetEvent eventInstance = Event;
            if (eventInstance != null)
            {// Release the waiting thread.
                eventInstance.Set();
            }
        }

        #endregion
    }
}
