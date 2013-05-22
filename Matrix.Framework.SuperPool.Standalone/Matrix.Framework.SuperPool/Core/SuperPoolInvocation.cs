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
using Matrix.Framework.MessageBus.Core;
using System.Collections.Generic;
using Matrix.Framework.SuperPool.Call;
using Matrix.Framework.SuperPool.Clients;
using Matrix.Framework.SuperPool.DynamicProxy;
using Matrix.Common.Core.Collections;
using Matrix.Common.Core.Identification;

namespace Matrix.Framework.SuperPool.Core
{
    /// <summary>
    /// Common callbacks/events - accessible both trough events and trough callbacks with attributes.
    /// Item added, item removed, item changed operational state.
    /// Item operation executed, item event raised.
    /// </summary>
    public abstract class SuperPoolInvocation : SuperPoolClients
    {
        public static bool CallContextEnabled = true;

        /// <summary>
        /// The type builder instance.
        /// </summary>
        protected ProxyTypeBuilder ProxyTypeBuilder
        {
            get
            {
                ProxyTypeManager typeManager = _proxyTypeManager;
                if (typeManager != null)
                {
                    return typeManager.Builder;
                }

                return null;
            }
        }

        /// <summary>
        /// Values inside the collection are reused to further optimize calls (no new object created)
        /// and also this allows to have the collection fairly static, and thus lock free hot swappable.
        /// 
        /// *Important* the system reuses thread ids (when a thraed dies), so the number of items 
        /// in the collection is not expected to grow indefinately.
        /// 
        /// This is a suitable *HOT SWAP*, since threads are reused.
        /// </summary>
        protected HotSwapDictionary<int, SuperPoolProxyCall> _pendingThreadsCalls = new HotSwapDictionary<int, SuperPoolProxyCall>();

        long _lastCallId = 0;

        /// <summary>
        /// Constructor.
        /// </summary>
        public SuperPoolInvocation()
        {
        }

        /// <summary>
        /// Dispose.
        /// </summary>
        public override void Dispose()
        {
            base.Dispose();
        }

        /// <summary>
        /// Obtain a unique long integer id for a call.
        /// Each call performed on the pool must have an id like this, 
        /// so it can be tracked if needed.
        /// </summary>
        /// <returns></returns>
        public long GetUniqueCallId()
        {
            return Interlocked.Increment(ref _lastCallId);
        }

        #region Public

        /// <summary>
        /// Helper version of the call, with no receiver speicified.
        /// This is utilized in events calls.
        /// </summary>
        internal bool Call<InterfaceType>(SuperPoolClient sender, out InterfaceType result, out SuperPoolProxyCall call)
            where InterfaceType : class
        {
            return Call<InterfaceType>(sender, (IEnumerable<ComponentId>)null, out result, out call);
        }

        /// <summary>
        /// 
        /// </summary>
        internal bool Call<InterfaceType>(SuperPoolClient sender, ComponentId receiversIds,
                                          out InterfaceType result, out SuperPoolProxyCall call)
            where InterfaceType : class
        {
            if (receiversIds == null)
            {
                return Call<InterfaceType>(sender, (IEnumerable<ComponentId>)null, out result, out call);
            }
            else
            {
                return Call<InterfaceType>(sender, new ComponentId[] { receiversIds }, out result, out call);
            }
        }

        /// <summary>
        /// Basic asynchronous call operation.
        /// </summary>
        /// <param name="receiverId">The value of the receiver, null means call all.</param>
        internal bool Call<InterfaceType>(SuperPoolClient sender, IEnumerable<ComponentId> receiversIds, 
                                          out InterfaceType result, out SuperPoolProxyCall call)
            where InterfaceType : class
        {
            call = null;
            result = null;

            // SLOW.
            //if (_messageBus.ContainsClient(sender.Id) == false)
            //{
            //    SystemMonitor.OperationError("Client not a member of message bus (and super pool).");
            //    return false;
            //}

            if (typeof(InterfaceType).IsInterface == false)
            {
                throw new Exception("Type provided not an interface.");
            }

            // Very slow !!
            //object[] attributes = typeof(InterfaceType).GetCustomAttributes(typeof(SuperPoolInterfaceAttribute), false);
            //if (attributes == null || attributes.Length == 0)
            //{
            //    SystemMonitor.Throw("Interface type [" + typeof(InterfaceType).Name + "] not marked as super pool interface.");
            //    return false;
            //}

            ProxyTypeManager typeManager = _proxyTypeManager;
            if (typeManager == null)
            {
                return false;
            }

            if (_pendingThreadsCalls.TryGetValue(Thread.CurrentThread.ManagedThreadId, out call) == false)
            {// We are safe from danger of someone else already adding the value with this id,
                // since we are the only thread with this id.
                call = new SuperPoolProxyCall();
                // This is slow, but very rarely executed, since thread ids are reused.
                _pendingThreadsCalls.Add(Thread.CurrentThread.ManagedThreadId, call);
            }
            else
            {
                // Since we reuse the call, clean it up before usage.
                call.Clear();
            }

            call.Processed = false;
            if (receiversIds != null)
            {// Extract the Indeces from the Ids.
                List<ClientId> receiversIndeces = new List<ClientId>();
                foreach (ComponentId id in receiversIds)
                {
                    receiversIndeces.Add((ClientId)id);
                }

                call.ReceiversIds = receiversIndeces;
            }
            else
            {
                call.ReceiversIds = null;
            }

            call.Sender = sender;
            
            result = (InterfaceType)typeManager.ObtainInterfaceProxy(typeof(InterfaceType));

            return true;
        }


        #endregion

    }
}
