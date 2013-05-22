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
using System.Reflection;
using Matrix.Common.Core.Collections;

namespace Matrix.Framework.SuperPool.DynamicProxy
{
    /// <summary>
    /// Class manages the operation of dynamically built proxy type instances and types.
    /// Operation is separated as follows:
    /// - proxy class, implemetns a proxy instance of a client instance.
    /// </summary>
    public class ProxyTypeManager : IDisposable
    {
        private ProxyTypeBuilder _builder = new ProxyTypeBuilder("ProxyTypeManager.Proxies");

        Dictionary<Type, object> _proxyObjects = new Dictionary<Type, object>();

        /// <summary>
        /// The builder we used to create the proxy types.
        /// </summary>
        public ProxyTypeBuilder Builder
        {
            get { return _builder; }
        }

        /// <summary>
        /// All reference types are automatically defaulted to null, so this only handles the value types.
        /// </summary>
        static HotSwapDictionary<Type, object> _defaultReturnValues = new HotSwapDictionary<Type, object>();

        /// <summary>
        /// Static constructor.
        /// </summary>
        static ProxyTypeManager()
        {
            _defaultReturnValues.AddRange(
                new Type[] { typeof(Int16), typeof(Int32), typeof(Int64), typeof(uint), typeof(double), typeof(string) },
                new object[] { 0, 0, 0, 0, 0, string.Empty });
        }

        IProxyTypeSink _sink;

        public IProxyTypeSink Sink
        {
            get { return _sink; }
            set { _sink = value; }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public ProxyTypeManager()
        {
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual void Dispose()
        {
        }

        /// <summary>
        /// Helper.
        /// </summary>
        protected void Abort(string message)
        {
            throw new Exception(message);
        }

        #region Public 

        ///// <summary>
        ///// Obtain a proxy method delegate for a dynamic method.
        ///// Obtain only once, when we attach it to the event.
        ///// </summary>
        //public GeneratedDynamicMethodInfo ObtainDynamicMethodProxyDelegate(Type delegateType)
        //{
        //    // Establish delegate parameters.
        //    MethodInfo delegateMethodInfo = delegateType.GetMethod("Invoke");

        //    // Generate a new dynamic method; we can *NOT REUSE THEM*, since they work on handling events
        //    // and each event handler must be traceable to the instance that we subscribed it for.
        //    return _builder.GenerateDynamicProxyMethod(ProxyTypeBuilder.GetMethodParametersTypes(delegateMethodInfo), 
        //        delegateMethodInfo.ReturnType);

        //    //lock (_dynamicMethods)
        //    //{
        //    //    if (_dynamicMethods.ContainsKey(delegateMethodInfo))
        //    //    {
        //    //        return _dynamicMethods[delegateMethodInfo];
        //    //    }

        //    //    Delegate delegateInstance = dynamicMethod.Method.CreateDelegate(delegateType, _dynamicMethodSink);
                
        //    //    //object result = delegateInstance2.DynamicInvoke(0);
        //    //    //delegateStatic.DynamicInvoke(_dynamicMethodSink, 0);
        //    //    //delegateInstance.DynamicInvoke(null, EventArgs.Empty);

        //    //    //return delegateInstance;

        //    //    //// Lock it, since this way we shall only create what we actually use.
        //    //    //object proxy = Activator.CreateInstance(dynamicMethod.GeneratedType, _sink);

        //    //    //Delegate delegateInstance = Delegate.CreateDelegate(delegateType, proxy,
        //    //    //    dynamicMethod.GeneratedType.GetMethod(delegateMethodInfo.Name));

        //    //    //_dynamicMethods[delegateMethodInfo] = delegateInstance;

        //    //    //return delegateInstance;
        //    //    return delegateInstance;
        //    //}

        //}

        /// <summary>
        /// Invoked on each call.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public object ObtainInterfaceProxy(Type type)
        {
            lock (_proxyObjects)
            {
                if (_proxyObjects.ContainsKey(type))
                {
                    return _proxyObjects[type];
                }
            }

            if (type.IsInterface == false)
            {
                Abort("Type [" + type.Name + "] not an interface.");
            }

            Type proxyType = _builder.GenerateInterfaceProxyImplementation(type);
            if (proxyType == null)
            {// Failed to create proxy type for this interface.
                return null;
            }

            ConstructorInfo info = proxyType.GetConstructor(new Type[] { typeof(IProxyTypeSink) });
            object proxyInstance = info.Invoke(new object[] { _sink });

            lock (_proxyObjects)
            {
                if (_proxyObjects.ContainsKey(type) == false)
                {
                    _proxyObjects.Add(type, proxyInstance);
                }
            }

            return proxyInstance;
        }

        #endregion

        /// <summary>
        /// Implementation, separate in order to allow calls from child classes.
        /// </summary>
        public static object GetTypeDefaultValue(Type returnType)
        {
            if (returnType == null || returnType.IsByRef
                || returnType.IsClass)
            {
                return null;
            }

            object result;
            if (_defaultReturnValues.TryGetValue(returnType, out result))
            {
                return result;
            }

            result = Activator.CreateInstance(returnType);
            _defaultReturnValues.Add(returnType, result);

            return result;
        }

    }
}
