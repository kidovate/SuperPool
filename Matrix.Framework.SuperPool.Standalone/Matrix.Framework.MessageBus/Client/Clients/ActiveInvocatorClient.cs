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
using System.Reflection;
using Matrix.Common.Core;
using Matrix.Framework.MessageBus.Core;

#if Matrix_Diagnostics
using Matrix.Common.Diagnostics;
#endif

namespace Matrix.Framework.MessageBus.Clients
{
    /// <summary>
    /// Class extends default client functionality with ability to send invokes directly to consumer object methods,
    /// using a fast invoker and reflection to do so.
    /// 
    /// Optional handling parameters: IMessageBusClient, Envelope
    /// </summary>
    public class ActiveInvocatorClient : ActiveClient
    {
        /// <summary>
        /// Describe and implement the handling capabilities related to a specific type of message.
        /// </summary>
        internal class TypeHandler
        {
            internal FastInvokeHelper.FastInvokeHandlerDelegate DelegateInstance;
            internal MessageBusClient ClientInstance = null;
            internal bool EnvelopeRequired = false;

            internal object Invoke(object target, Envelope envelope)
            {
                object[] parameters = null;
                if (ClientInstance != null && EnvelopeRequired)
                {
                    parameters = new object[] { ClientInstance, envelope, envelope.Message};
                }
                else if (ClientInstance != null && EnvelopeRequired == false)
                {
                    parameters = new object[] { ClientInstance, envelope.Message};
                }
                else if (ClientInstance == null && EnvelopeRequired)
                {
                    parameters = new object[] { envelope, envelope.Message};
                }
                else if (ClientInstance == null && EnvelopeRequired == false)
                {
                    parameters = new object[] { envelope.Message};
                }

                return DelegateInstance(target, parameters);
            }
        }

        volatile Dictionary<Type, TypeHandler> _typeHandlersHotSwap = new Dictionary<Type, TypeHandler>();

        volatile object _source = null;
        /// <summary>
        /// The instance that is supposed to consume the messages.
        /// </summary>
        public virtual object Source
        {
            get
            {
                return _source;
            }

            set
            {
                _source = value;

                if (_source != null)
                {
                    List<MethodInfo> receiverMethods = ReflectionHelper.GatherTypeMethodsByAttribute(_source.GetType(), typeof(EnvelopeReceiverAttribute),
                                                                                             BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, true);

                    Dictionary<Type, TypeHandler> typeHandlersHotSwap = new Dictionary<Type, TypeHandler>();

                    foreach (MethodInfo methodInfo in receiverMethods)
                    {
                        TypeHandler handler = null;
                        Type typeHandled = null;

                        ParameterInfo[] parameters = methodInfo.GetParameters();
                        if (parameters.Length == 3
                            && (parameters[0].ParameterType == typeof(MessageBusClient))
                            && (parameters[1].ParameterType == typeof(Envelope)))
                        {// Handling the client instance plus the envelope plus the parameter.
                            typeHandled = parameters[2].ParameterType;
                            handler = new TypeHandler() { EnvelopeRequired = true, ClientInstance = this, DelegateInstance = FastInvokeHelper.GetMethodInvoker(methodInfo, true, false) };
                        }
                        else if (parameters.Length == 2 &&
                                 (parameters[0].ParameterType == typeof(MessageBusClient)))
                        {// Handling the client instance plus the parameter.
                            typeHandled = parameters[1].ParameterType;
                            handler = new TypeHandler() { EnvelopeRequired = false, ClientInstance = this, DelegateInstance = FastInvokeHelper.GetMethodInvoker(methodInfo, true, false) };
                        }
                        else if (parameters.Length == 2 &&
                                 (parameters[0].ParameterType == typeof(Envelope)))
                        {// Handling the client instance plus the parameter.
                            typeHandled = parameters[1].ParameterType;
                            handler = new TypeHandler() { EnvelopeRequired = true, ClientInstance = null, DelegateInstance = FastInvokeHelper.GetMethodInvoker(methodInfo, true, false) };
                        }
                        else if (parameters.Length == 1)
                        {// Hanlidng the parameter only.
                            typeHandled = parameters[0].ParameterType;
                            handler = new TypeHandler() { DelegateInstance = FastInvokeHelper.GetMethodInvoker(methodInfo, true, false) };
                        }

                        if (handler == null || typeHandled == null)
                        {
#if Matrix_Diagnostics
                            SystemMonitor.Error("Failed to establish parameter format for method [" + methodInfo.Name + ", type " + _source.GetType().Name + "].");
#endif
                        }
                        else
                        {
                            typeHandlersHotSwap.Add(typeHandled, handler);
                        }
                    }

                    // Hot swap the 2 instances.
                    _typeHandlersHotSwap = typeHandlersHotSwap;
                }
                else
                {
                    _typeHandlersHotSwap = new Dictionary<Type, TypeHandler>();
                }
            }
        }

        public override Type OptionalSourceType
        {
            get
            {
                object source = _source;
                if (source != null)
                {
                    return source.GetType();
                }

                return null;
            }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public ActiveInvocatorClient(string name)
            : base(name)
        { }

        /// <summary>
        /// Constructor.
        /// </summary>
        public ActiveInvocatorClient(ClientId id)
            : base(id)
        { }

        public override void Dispose()
        {
            this.Source = null;
            base.Dispose();
        }

        protected override void OnPerformExecution(Envelope envelope)
        {
            Dictionary<Type, TypeHandler> typeHandlersHotSwap = _typeHandlersHotSwap;
            Type messageType = envelope.Message.GetType();
            object target = _source;
            if (target != null && envelope.Message != null && typeHandlersHotSwap != null)
            {
                TypeHandler handler;
                if (typeHandlersHotSwap.TryGetValue(messageType, out handler))
                {
                    handler.Invoke(target, envelope);
                }
            }
        }

    }
}
