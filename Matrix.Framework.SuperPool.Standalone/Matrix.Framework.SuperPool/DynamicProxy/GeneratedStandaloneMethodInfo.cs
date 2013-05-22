// -----
// Copyright 2010 Deyan Timnev
// This file is part of the Matrix Platform (www.matrixplatform.com).
// The Matrix Platform is free software: you can redistribute it and/or modify it under the terms of the GNU Lesser General Public License as published by the Free Software Foundation, 
// either version 3 of the License, or (at your option) any later version. The Matrix Platform is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; 
// without even the implied warranty of  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Lesser General Public License for more details.
// You should have received a copy of the GNU Lesser General Public License along with the Matrix Platform. If not, see http://www.gnu.org/licenses/lgpl.html
// -----
using System;
using System.Reflection.Emit;
using System.Reflection;

#if Matrix_Diagnostics
using Matrix.Common.Diagnostics;
#endif

namespace Matrix.Framework.SuperPool.DynamicProxy
{
    /// <summary>
    /// Stores info on a single standalone method, or a method generated as a part of proxy class.
    /// The method is what we need, and the class stores it
    /// and provides it with member instances.
    /// </summary>
    public class GeneratedMethodInfo
    {
        #region Common

        public int Id { get; protected set; }

        /// <summary>
        /// Assigned trough usage by a Super pool component.
        /// </summary>
        public string EventName { get; set; }

        #endregion

        #region Standalone specific

        public bool IsStandalone
        {
            get
            {
                return StandaloneDynamicMethod != null;
            }
        }

        /// <summary>
        /// The dynamic method we generated
        /// </summary>
        public DynamicMethod StandaloneDynamicMethod { get; protected set; }

        #endregion
        
        #region Part of proxy specific

        public bool IsProxyClassMethod
        {
            get
            {
                return ProxyOwnerType != null;
            }   
        }


        /// <summary>
        /// The owner proxy type (if available).
        /// </summary>
        public Type ProxyOwnerType { get; protected set; }

        /// <summary>
        /// The method info we implement upon.
        /// </summary>
        public MethodInfo ProxyMethodInfo { get; protected set; }

        #endregion

        /// <summary>
        /// Construct proxy class method.
        /// </summary>
        public GeneratedMethodInfo(int id, Type proxyType, MethodInfo methodInfo)
        {
            Id = id;
            ProxyMethodInfo = methodInfo;
            ProxyOwnerType = proxyType;
        }

        /// <summary>
        /// Construct standalone.
        /// </summary>
        public GeneratedMethodInfo(int id, DynamicMethod dynamicMethod, Type delegateType)
        {
            this.Id = id;

            //this.StandaloneDelegateType = delegateType;
            this.StandaloneDynamicMethod = dynamicMethod;
        }

        /// <summary>
        /// This operates on valid object in both a Standalone and Proxy method modes.
        /// </summary>
        public MethodInfo GetMethodInfo()
        {
            if (IsProxyClassMethod)
            {
                return ProxyMethodInfo;
            }
            else if (IsStandalone)
            {
                return StandaloneDynamicMethod;
            }

            return null;
        }

        /// <summary>
        /// Only works for ProxyTypes, since stand alone methods do not have it.
        /// </summary>
        /// <returns></returns>
        public Type GetBaseInterfaceType()
        {
            Type proxyType = ProxyOwnerType;
            Type[] interfaceTypes = proxyType.GetInterfaces();

            if (interfaceTypes.Length != 1)
            {
#if Matrix_Diagnostics
                SystemMonitor.Error(string.Format("Proxy class [{0}] does not provide clear interface specification.", proxyType.ToString()));
#endif
                return null;
            }

            return interfaceTypes[0];
        }

        public override string ToString()
        {
            MethodInfo methodInfo = GetMethodInfo();
            if (methodInfo == null)
            {// Instance not initialized properly yet.
                return base.ToString();
            }

            if (IsStandalone)
            {
                return "Standalone method [" + methodInfo.ToString() + "]";
            }
            else
            {
                Type baseInterfaceType = GetBaseInterfaceType();
                return "Generated method info for [" + baseInterfaceType.Name + "." + methodInfo.ToString() + "]";
            }
        }
    }
}
