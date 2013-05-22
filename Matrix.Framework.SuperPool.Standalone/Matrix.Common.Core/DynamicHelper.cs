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

namespace Matrix.Common.Core
{
    /// <summary>
    /// Assists with dynamic invocation.
    /// Allows fully dynamic operations in the runtime, much like the "dynamic" keyword in .NET 4.0
    /// but only requires .NET 2.0 to operate.
    /// </summary>
    public static class DynamicHelper
    {
        /// <summary>
        /// Perform a dynamic call.
        /// Able to process advanced calls like this myObject.Call("SomeProp.Method1")
        /// </summary>
        public static object Call(object source, string valueName, params object[] parameters)
        {
            string[] subValues = valueName.Split('.');
            object pendingSource = source;
            for (int i = 0; i < subValues.Length; i++)
            {
                if (i == subValues.Length - 1)
                {// Last call, supply parameters.
                    return PerformCall(pendingSource, subValues[i], parameters);
                }
                else
                {
                    pendingSource = PerformCall(pendingSource, subValues[i]);
                }
            }

            return null;
        }

        /// <summary>
        /// Gets the value dynamically from a non-typed object, by trying to access a method or 
        /// property with this name.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="valueName"></param>
        /// <returns>Null if the method was not found.</returns>
        static object PerformCall(object source, string valueName, params object[] parameters)
        {
            Type[] parametersTypes = null;
            if (parameters != null && parameters.Length > 0)
            {
                parametersTypes = new Type[parameters.Length];
                for (int i = 0; i < parameters.Length; i++)
                {
                    parametersTypes[i] = parameters[i].GetType();
                }
            }

            Type type = source.GetType();

            MethodInfo mi;
            if (parametersTypes != null)
            {
                mi = type.GetMethod(valueName, parametersTypes);
            }
            else
            {
                mi = type.GetMethod(valueName, new Type[] { });
            }

            if (mi != null)
            {
                return mi.Invoke(source, parameters);
            }

            if (parameters != null && parameters.Length > 1)
            {// Method not found.
                return null;
            }

            PropertyInfo pi = type.GetProperty(valueName);
            if (pi == null)
            {// Property not found.
                return null;
            }

            if (parameters != null && parameters.Length == 1)
            {// Property set.
                pi.SetValue(source, parameters[0], null);
                return null;
            }
            else
            {// Property get.
                return pi.GetValue(source, null);
            }
        }
    }
}
