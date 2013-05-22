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
using System.Reflection.Emit;
using System.Reflection;

namespace Matrix.Common.Core
{
    /// <summary>
    /// This class uses runtime code generation, to overcome the problem of slow runtime invocation.
    /// A new method is generated on runtime and calls are passed trough it.
    /// As seen here:
    /// http://www.codeproject.com/KB/cs/FastMethodInvoker.aspx
    /// </summary>
    public static class FastInvokeHelper
    {
        /// <summary>
        /// The caching mechanism used, allows to reuse Delegate instances
        /// accross calls over time; and is thus rather fast.
        /// </summary>
        static Dictionary<MethodInfo, FastInvokeHandlerDelegate> _cache = new Dictionary<MethodInfo, FastInvokeHandlerDelegate>();

        /// <summary>
        /// The delegate that is used to perform the actual fast invocation.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public delegate object FastInvokeHandlerDelegate(object target, object[] parameters);

        /// <summary>
        /// Perform an invoke, using a fast delegate. Will try to use the delegate
        /// cache, so only the first call will be delayed while the delegate is generated.
        /// </summary>
        public static object CachedInvoke(MethodInfo methodInfo, object target, object[] parameters)
        {
            FastInvokeHandlerDelegate delegateInstance = GetMethodInvoker(methodInfo, true, true);
            return delegateInstance.Invoke(target, parameters);
        }

        /// <summary>
        /// This method generates the new method on runtime and passes a delegate to it.
        /// The actual generation is a *slow process* (only the very first run, if cache is used), so make sure to save the delegate
        /// for later usage, instead of generating each time.
        /// </summary>
        /// <param name="methodInfo"></param>
        /// <returns></returns>
        public static FastInvokeHandlerDelegate GetMethodInvoker(MethodInfo methodInfo, bool useCache, bool skipVisibility)
        {
            if (useCache)
            {
                lock (_cache)
                {
                    FastInvokeHandlerDelegate result = null;
                    if (_cache.TryGetValue(methodInfo, out result))
                    {
                        return result;
                    }
                }
            }

            DynamicMethod dynamicMethod = new DynamicMethod(string.Empty, typeof(object), new Type[] { typeof(object), typeof(object[]) }, methodInfo.DeclaringType.Module, skipVisibility);

            ILGenerator il = dynamicMethod.GetILGenerator();
            ParameterInfo[] ps = methodInfo.GetParameters();

            Type[] paramTypes = new Type[ps.Length];

            for (int i = 0; i < paramTypes.Length; i++)
            {
                if (ps[i].ParameterType.IsByRef)
                {
                    paramTypes[i] = ps[i].ParameterType.GetElementType();
                }
                else
                {
                    paramTypes[i] = ps[i].ParameterType;
                }
            }

            LocalBuilder[] locals = new LocalBuilder[paramTypes.Length];

            for (int i = 0; i < paramTypes.Length; i++)
            {
                locals[i] = il.DeclareLocal(paramTypes[i], true);
            }

            for (int i = 0; i < paramTypes.Length; i++)
            {
                il.Emit(OpCodes.Ldarg_1);
                EmitFastInt(il, i);

                il.Emit(OpCodes.Ldelem_Ref);
                EmitCastToReference(il, paramTypes[i]);
                il.Emit(OpCodes.Stloc, locals[i]);
            }

            if (!methodInfo.IsStatic)
            {
                il.Emit(OpCodes.Ldarg_0);
            }

            for (int i = 0; i < paramTypes.Length; i++)
            {
                if (ps[i].ParameterType.IsByRef)
                {
                    il.Emit(OpCodes.Ldloca_S, locals[i]);
                }
                else
                {
                    il.Emit(OpCodes.Ldloc, locals[i]);
                }
            }

            if (methodInfo.IsStatic)
                il.EmitCall(OpCodes.Call, methodInfo, null);
            else
                il.EmitCall(OpCodes.Callvirt, methodInfo, null);
            if (methodInfo.ReturnType == typeof(void))
                il.Emit(OpCodes.Ldnull);
            else
                EmitBoxIfNeeded(il, methodInfo.ReturnType);

            for (int i = 0; i < paramTypes.Length; i++)
            {
                if (ps[i].ParameterType.IsByRef)
                {
                    il.Emit(OpCodes.Ldarg_1);
                    EmitFastInt(il, i);
                    il.Emit(OpCodes.Ldloc, locals[i]);
                    if (locals[i].LocalType.IsValueType)
                        il.Emit(OpCodes.Box, locals[i].LocalType);
                    il.Emit(OpCodes.Stelem_Ref);
                }
            }

            il.Emit(OpCodes.Ret);
            FastInvokeHandlerDelegate invoder = (FastInvokeHandlerDelegate)dynamicMethod.CreateDelegate(typeof(FastInvokeHandlerDelegate));

            if (useCache)
            {
                lock (_cache)
                {// Possible multiple entry.
                    _cache[methodInfo] = invoder;
                }
            }

            return invoder;
        }

        private static void EmitCastToReference(ILGenerator il, System.Type type)
        {
            if (type.IsValueType)
            {
                il.Emit(OpCodes.Unbox_Any, type);
            }
            else
            {
                il.Emit(OpCodes.Castclass, type);
            }
        }

        private static void EmitBoxIfNeeded(ILGenerator il, System.Type type)
        {
            if (type.IsValueType)
            {
                il.Emit(OpCodes.Box, type);
            }
        }

        private static void EmitFastInt(ILGenerator il, int value)
        {
            switch (value)
            {
                case -1:
                    il.Emit(OpCodes.Ldc_I4_M1);
                    return;
                case 0:
                    il.Emit(OpCodes.Ldc_I4_0);
                    return;
                case 1:
                    il.Emit(OpCodes.Ldc_I4_1);
                    return;
                case 2:
                    il.Emit(OpCodes.Ldc_I4_2);
                    return;
                case 3:
                    il.Emit(OpCodes.Ldc_I4_3);
                    return;
                case 4:
                    il.Emit(OpCodes.Ldc_I4_4);
                    return;
                case 5:
                    il.Emit(OpCodes.Ldc_I4_5);
                    return;
                case 6:
                    il.Emit(OpCodes.Ldc_I4_6);
                    return;
                case 7:
                    il.Emit(OpCodes.Ldc_I4_7);
                    return;
                case 8:
                    il.Emit(OpCodes.Ldc_I4_8);
                    return;
            }

            if (value > -129 && value < 128)
            {
                il.Emit(OpCodes.Ldc_I4_S, (SByte)value);
            }
            else
            {
                il.Emit(OpCodes.Ldc_I4, value);
            }
        }


    }
}
