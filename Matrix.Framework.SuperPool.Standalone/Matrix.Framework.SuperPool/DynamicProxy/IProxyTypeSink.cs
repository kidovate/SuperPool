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

namespace Matrix.Framework.SuperPool.DynamicProxy
{
    /// <summary>
    /// Interface serves as a sink for proxied calls.
    /// 
    /// *IMPORTANT* renaming any of these methods *must* be matched in the ProxyTypeBuilder.
    /// </summary>
    public interface IProxyTypeSink
    {
        void ReceiveMethodCall(int methodId, object[] parameters);
        object ReceiveMethodCallAndReturn(int methodId, Type returnType, object[] parameters);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="methodId"></param>
        /// <param name="returnType">Usefull to return a default value when failed to establish a critical component.</param>
        /// <returns></returns>
        object ReceivePropertyGet(int methodId, Type returnType);
        void ReceivePropertySet(int methodId, object value);

        void ReceiveEventSubscribed(int methodId, Delegate subscribedDelegate);
        void ReceiveEventUnSubscribed(int methodId, Delegate subscribedDelegate);
    }
}
