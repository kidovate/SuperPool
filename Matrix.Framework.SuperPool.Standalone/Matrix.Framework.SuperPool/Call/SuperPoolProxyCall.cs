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
using Matrix.Framework.MessageBus.Core;
using Matrix.Framework.SuperPool.Clients;
using Matrix.Framework.SuperPool.DynamicProxy;
using Matrix.Framework.SuperPool.Subscription;

namespace Matrix.Framework.SuperPool.Call
{
    /// <summary>
    /// Stores information related to a single call to a super pool proxy.
    /// </summary>
    public class SuperPoolProxyCall
    {
        public enum ModeEnum : int
        {
            Default,
            DirectCall,
            CallFirst
        }

        public ModeEnum Mode { get; set; }

        public bool Processed { get; set; }

        public TimeSpan? RequestConfirmTimeout = null;

        public int? MethodId { get; set; }
        public Type ReturnType { get; set; }
        public object[] Parameters { get; set; }

        public GeneratedMethodInfo MethodInfo { get; set; }

        public List<ClientId> ReceiversIds { get; set; }

        public SuperPoolClient Sender { get; set; }

        public TimeSpan? Timeout { get; set; }

        public bool IsSynchronous
        {
            get { return Timeout.HasValue; }
        }

        public bool IsAsyncResultExpecting
        {
            get { return AsyncResultDelegate != null; }
        }

        internal EventSubscriptionRequest SubscriptionRequest { get; set; }

        internal CallOutcome Outcome { get; set; }

        public AsyncCallResultDelegate AsyncResultDelegate { get; set; }
        
        public object AsyncResultState { get; set; }

        /// <summary>
        /// Used when a call was made to any implementor, and results are collection in 
        /// async fashion, with this max. timeout allowed for a result.
        /// </summary>
        public TimeSpan? AsyncResultTimeout { get; set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        public SuperPoolProxyCall()
        {
            Clear();
        }

        /// <summary>
        /// Clear instance.
        /// </summary>
        public void Clear()
        {
            Mode = ModeEnum.Default;

            Processed = false;
            RequestConfirmTimeout = null;

            SubscriptionRequest = null;

            MethodId = null;
            ReturnType = null;
            Parameters = null;
            MethodInfo = null;
            ReceiversIds = null;
            Sender = null;
            Timeout = null;
            
            AsyncResultDelegate = null;
            AsyncResultState = null;
            AsyncResultTimeout = null;
        }

        public override string ToString()
        {
            if (IsSynchronous)
            {
                return string.Format(base.ToString() + ", from [{0}, {1}].", Sender.Id.Name, MethodInfo != null ? MethodInfo.ToString() : string.Empty);
            }
            else
            {
                return string.Format(base.ToString() + ", from [{0}, {1}].", Sender.Id.Name, MethodInfo != null ? MethodInfo.ToString() : string.Empty);
            }
        }

    }
}
