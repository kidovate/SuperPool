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
using System.Threading;
using Matrix.Framework.SuperPool.Call;
using Matrix.Framework.SuperPool.DynamicProxy;

namespace Matrix.Framework.SuperPool.Core
{
    /// <summary>
    /// Message super pool class layer, handles incoming proxy type sink callbacks.
    /// </summary>
    public abstract class SuperPoolCallbacks : SuperPoolInvocation, IProxyTypeSink
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public SuperPoolCallbacks()
        {
            _proxyTypeManager.Sink = this;
        }

        protected abstract void ProcessReceiveEventSubscription(int methodId, Delegate delegateInstance, bool isAdd);

        #region IProxyTypeSink Members

        void IProxyTypeSink.ReceiveMethodCall(int methodId, object[] parameters)
        {
            ProcessReceiveCall(methodId, null, parameters);
        }

        object IProxyTypeSink.ReceiveMethodCallAndReturn(int methodId, Type returnType, object[] parameters)
        {
            return ProcessReceiveCall(methodId, returnType, parameters);
        }

        void IProxyTypeSink.ReceiveEventSubscribed(int methodId, Delegate subscribedDelegate)
        {
            ProcessReceiveEventSubscription(methodId, subscribedDelegate, true);
        }

        void IProxyTypeSink.ReceiveEventUnSubscribed(int methodId, Delegate subscribedDelegate)
        {
            ProcessReceiveEventSubscription(methodId, subscribedDelegate, false);
        }

        object IProxyTypeSink.ReceivePropertyGet(int methodId, Type returnType)
        {
            SuperPoolProxyCall pendingCall = null;
            if (_pendingThreadsCalls.TryGetValue(Thread.CurrentThread.ManagedThreadId, out pendingCall) == false)
            {
#if Matrix_Diagnostics
                InstanceMonitor.OperationError("Failed to find corresponding thread proxy call information.");
#endif
                return null;
            }

            pendingCall.Parameters = null;
            pendingCall.ReturnType = returnType;

            ProxyTypeBuilder builder = ProxyTypeBuilder;
            if (builder == null)
            {
#if Matrix_Diagnostics
                InstanceMonitor.OperationError("Failed to find proxy type builder.");
#endif
                return ProxyTypeManager.GetTypeDefaultValue(returnType);
            }

            pendingCall.MethodInfo = builder.GetMethodInfoById(methodId);
            if (pendingCall.MethodInfo == null)
            {
#if Matrix_Diagnostics
                InstanceMonitor.OperationError("Failed to find method [" + methodId + "] info.");
#endif
                return ProxyTypeManager.GetTypeDefaultValue(returnType);
            }

            return pendingCall.Sender.ProcessCall(pendingCall);
        }

        void IProxyTypeSink.ReceivePropertySet(int methodId, object value)
        {
            SuperPoolProxyCall pendingCall = null;
            if (_pendingThreadsCalls.TryGetValue(Thread.CurrentThread.ManagedThreadId, out pendingCall) == false)
            {
#if Matrix_Diagnostics
                InstanceMonitor.OperationError("Failed to find corresponding thread proxy call information.");
#endif
                return;
            }

            pendingCall.Parameters = new object[] { value };
            pendingCall.ReturnType = null;

            ProxyTypeBuilder builder = ProxyTypeBuilder;
            if (builder == null)
            {
#if Matrix_Diagnostics
                InstanceMonitor.OperationError("Failed to find proxy type builder.");
#endif
                return;
            }

            pendingCall.MethodInfo = builder.GetMethodInfoById(methodId);
            if (pendingCall.MethodInfo == null)
            {
#if Matrix_Diagnostics
                InstanceMonitor.OperationError("Failed to find method [" + methodId + "] info.");
#endif
                return;
            }

            pendingCall.Sender.ProcessCall(pendingCall);
        }


        #endregion

        /// <summary>
        /// Process a pool call.
        /// </summary>
        object ProcessReceiveCall(int methodId, Type returnType, object[] parameters)
        {
            SuperPoolProxyCall pendingCall = null;
            if (_pendingThreadsCalls.TryGetValue(Thread.CurrentThread.ManagedThreadId, out pendingCall) == false)
            {
#if Matrix_Diagnostics
                InstanceMonitor.OperationError("Failed to find corresponding thread proxy call information.");
#endif
                return null;
            }

            pendingCall.Parameters = parameters;
            pendingCall.ReturnType = returnType;

            ProxyTypeBuilder builder = ProxyTypeBuilder;
            if (builder == null)
            {
#if Matrix_Diagnostics
                InstanceMonitor.OperationError("Failed to find proxy type builder.");
#endif
                return ProxyTypeManager.GetTypeDefaultValue(returnType);
            }

            pendingCall.MethodInfo = builder.GetMethodInfoById(methodId);
            if (pendingCall.MethodInfo == null)
            {
#if Matrix_Diagnostics
                InstanceMonitor.OperationError("Failed to find method [" + methodId + "] info.");
#endif
                return ProxyTypeManager.GetTypeDefaultValue(returnType);
            }

            return pendingCall.Sender.ProcessCall(pendingCall);
        }

    }
}
