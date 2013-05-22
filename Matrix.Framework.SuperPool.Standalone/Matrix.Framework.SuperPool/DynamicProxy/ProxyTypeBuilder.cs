// -----
// Copyright 2010 Deyan Timnev
// This file is part of the Matrix Platform (www.matrixplatform.com).
// The Matrix Platform is free software: you can redistribute it and/or modify it under the terms of the GNU Lesser General Public License as published by the Free Software Foundation, 
// either version 3 of the License, or (at your option) any later version. The Matrix Platform is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; 
// without even the implied warranty of  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Lesser General Public License for more details.
// You should have received a copy of the GNU Lesser General Public License along with the Matrix Platform. If not, see http://www.gnu.org/licenses/lgpl.html
// -----
using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Generic;
using System.Threading;
using Matrix.Common.Core.Collections;

#if Matrix_Diagnostics
using Matrix.Common.Diagnostics;
#endif

namespace Matrix.Framework.SuperPool.DynamicProxy
{
    /// <summary>
    /// *Code generation*
    /// - Push the last value on the stack before returning, to return it.
    /// - After a call or a set, the stack is emptied (?!)
    /// 
    /// + EACH METHOD GENERATED HAS AN ID (INT ONLY) AND TROUGH IT, WE ARE ABLE TO OBTAIN ITS METHODINFO MUCH FASTER
    /// </summary>
    public class ProxyTypeBuilder
    {
        AssemblyName _assemblyName = null;
        AssemblyBuilder _assemblyBuilder = null;
        ModuleBuilder _moduleBuilder = null;
        
        Dictionary<Type, Type> _typeProxyList = new Dictionary<Type, Type>();


        /// <summary>
        /// Both standalone and proxy member types stored here.
        /// Make sure *NOT* to remove elements, since we are using this for the PendingId as well, and all ids are indeces.
        /// </summary>
        HotSwapList<GeneratedMethodInfo> _methods = new HotSwapList<GeneratedMethodInfo>();

        /// <summary>
        /// This is used not only for methods, but also for dynamic nameless classes.
        /// </summary>
        protected int PendingDynamicId
        {
            get
            {
                lock (_methods)
                {
                    _methods.Add(null);
                    return _methods.Count - 1;
                }
            }
        }

        static MethodInfo _receiveCallMethodInfo = null;
        static MethodInfo _receiveCallAndReturnMethodInfo = null;
        static MethodInfo _getCurrentMethodInfo = null;

        static MethodInfo _receivePropertyGetMethodInfo = null;
        static MethodInfo _receivePropertySetMethodInfo = null;

        static MethodInfo _receiveEventSubscribedMethodInfo = null;
        static MethodInfo _receiveEventUnSubscribedMethodInfo = null;

        static MethodInfo _receiveDynamicMethodCallAndReturn = null;
        static MethodInfo _receiveDynamicMethodCall = null;

        /// <summary>
        /// Static constructor.
        /// </summary>
        static ProxyTypeBuilder()
        {
            _receiveCallMethodInfo = typeof(IProxyTypeSink).GetMethod("ReceiveMethodCall");
            _receiveCallAndReturnMethodInfo = typeof(IProxyTypeSink).GetMethod("ReceiveMethodCallAndReturn");

            _receivePropertyGetMethodInfo = typeof(IProxyTypeSink).GetMethod("ReceivePropertyGet");
            _receivePropertySetMethodInfo = typeof(IProxyTypeSink).GetMethod("ReceivePropertySet");

            _receiveEventSubscribedMethodInfo = typeof(IProxyTypeSink).GetMethod("ReceiveEventSubscribed");
            _receiveEventUnSubscribedMethodInfo = typeof(IProxyTypeSink).GetMethod("ReceiveEventUnSubscribed");

            _receiveDynamicMethodCall = typeof(IDynamicProxyMethodSink).GetMethod("ReceiveDynamicMethodCall");
            _receiveDynamicMethodCallAndReturn = typeof(IDynamicProxyMethodSink).GetMethod("ReceiveDynamicMethodCallAndReturn");

            _getCurrentMethodInfo = typeof(MethodBase).GetMethod("GetCurrentMethod");

            if (_receiveCallMethodInfo == null || _receiveCallAndReturnMethodInfo == null ||
                _receivePropertyGetMethodInfo == null || _receivePropertySetMethodInfo == null ||
                _receiveEventSubscribedMethodInfo == null || _receiveEventUnSubscribedMethodInfo == null ||
                _receiveDynamicMethodCall == null || _receiveDynamicMethodCallAndReturn == null)
            {
                throw new Exception("Failed to retrieve all method invocation informations.");
            }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public ProxyTypeBuilder(string assemblyName)
        {
            _assemblyName = new AssemblyName(assemblyName);
            _assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(_assemblyName, AssemblyBuilderAccess.RunAndSave);
            _moduleBuilder = _assemblyBuilder.DefineDynamicModule(_assemblyName.Name, _assemblyName.Name + ".dll");
        }

        /// <summary>
        /// Save will not store the dynamic methods.
        /// </summary>
        public void Save()
        {
            _assemblyBuilder.Save(_assemblyName.Name + ".dll");
        }

        /// <summary>
        /// Obtain method info based on methodId.
        /// </summary>
        public GeneratedMethodInfo GetMethodInfoById(int methodId)
        {
            GeneratedMethodInfo value = null;
            if (_methods.TryGetValue(methodId, ref value))
            {
                return value;
            }

            return null;
        }

        /// <summary>
        /// Helper.
        /// </summary>
        public static Type[] GetMethodParametersTypes(MethodInfo methodInfo)
        {
            ParameterInfo[] parameterInfos = methodInfo.GetParameters();
            Type[] parameters = new Type[parameterInfos.Length];

            for (int i = 0; i < parameterInfos.Length; i++)
            {
                parameters[i] = parameterInfos[i].ParameterType;
            }

            return parameters;
        }

        public static MethodBuilder CreateMethodFromMethodInfo(TypeBuilder typeBuilder, MethodInfo methodInfo)
        {
            MethodAttributes attributes = methodInfo.Attributes;
            attributes = MethodAttributes.Public | MethodAttributes.Virtual;

            return typeBuilder.DefineMethod(methodInfo.Name,
                                            MethodAttributes.Public | MethodAttributes.Virtual, methodInfo.ReturnType, GetMethodParametersTypes(methodInfo));
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="generator"></param>
        /// <param name="methodId"></param>
        /// <param name="sinkField"></param>
        /// <param name="isAdd"></param>
        public void GenerateProxyPropertyMethod(ILGenerator generator, int methodId, 
                                                Type propertyType, FieldBuilder sinkField, bool isGet)
        {
            // Load this (I think...).
            generator.Emit(OpCodes.Ldarg_0);

            // Load sink field.
            generator.Emit(OpCodes.Ldfld, sinkField);

            if (isGet)
            {
                if (propertyType == typeof(void))
                {// Not expected, get property with void.
                    throw new Exception("Void type not expected.");
                }
                
                // Load the methodId
                generator.Emit(OpCodes.Ldc_I4, (Int32)methodId);

                {// Load the return type parameter.
                    generator.Emit(OpCodes.Ldtoken, propertyType);
                    // Get the info for the Type.GetTypeFromHandle(), since we invoke it on runtime.
                    MethodInfo typeCallInfo = typeof(Type).GetMethod("GetTypeFromHandle");
                    generator.EmitCall(OpCodes.Call, typeCallInfo, null);
                    //generator.Emit(OpCodes.Ldnull);
                }

                // Call object ReceivePropertyGet().
                generator.EmitCall(OpCodes.Call, _receivePropertyGetMethodInfo, null);

                // Process return instance.
                if (propertyType.IsByRef)
                {// Reference types.
                    if (propertyType != typeof(object))
                    {
                        // Need to re-cast.
                        generator.Emit(OpCodes.Castclass, propertyType);
                    }
                }
                else
                {// Value types.
                    // Unbox
                    generator.Emit(OpCodes.Unbox_Any, propertyType);
                }
            }
            else
            {
                // Load the methodId
                generator.Emit(OpCodes.Ldc_I4, (Int32)methodId);

                // Load the value.
                generator.Emit(OpCodes.Ldarg_1);

                // We need to box value types, to pass as object.
                if (propertyType.IsByRef == false)
                {
                    // Box the type.
                    generator.Emit(OpCodes.Box, propertyType);
                }
                
                // Call void ReceivePropertySet().
                generator.EmitCall(OpCodes.Call, _receivePropertySetMethodInfo, null);
            }

            // Load the result.
            generator.Emit(OpCodes.Ret);
        }

        /// <summary>
        /// Safe to do it in runtime since it is fairly fast.
        /// 
        /// add
        /// {
        ///     // Safe to do it in runtime since it is fairly fast.
        ///     _sink.EventSubscribed(methodId, value);
        /// }
        /// 
        /// 
        /*{
         // Code size       20 (0x14)
          .maxstack  8
          IL_0000:  nop
          IL_0001:  ldarg.0
          IL_0002:  ldfld      class SuperPool.IProxyTypeSink SuperPool.ProxyTest::_sink
          IL_0007:  ldc.i4.2
          IL_0008:  ldarg.1
          IL_0009:  callvirt   instance void SuperPool.IProxyTypeSink::ReceiveEventSubscribed(int32,
                                                                                              class [mscorlib]System.Delegate)
          IL_000e:  nop
          IL_000f:  ret
        }
        */
        /// </summary>
        /// <param name="generator"></param>
        /// <param name="methodId"></param>
        /// <param name="sinkField"></param>
        /// <param name="targetMethodInfo"></param>
        public void GenerateProxyEventMethod(ILGenerator generator, int methodId, FieldBuilder sinkField, bool isAdd)
        {
            // Load parameter count.
            //generator.Emit(OpCodes.Nop);

            // Load this (I think...).
            generator.Emit(OpCodes.Ldarg_0);

            // Load sink field.
            generator.Emit(OpCodes.Ldfld, sinkField);

            // Load the methodId
            generator.Emit(OpCodes.Ldc_I4, (Int32)methodId);

            // Load the result.
            generator.Emit(OpCodes.Ldarg_1);

            if (isAdd)
            {
                // Call ReceiveEventSubscribed().
                generator.EmitCall(OpCodes.Call, _receiveEventSubscribedMethodInfo, null);
            }
            else
            {
                // Call ReceiveEventUnSubscribed().
                generator.EmitCall(OpCodes.Call, _receiveEventUnSubscribedMethodInfo, null);
            }

            // Load the result.
            generator.Emit(OpCodes.Ret);
        }

        /// <summary>
        /// void ProxyMethodImplementation(parameters)
        /// {
        /// }
        /* 
          .maxstack  3
          .locals init ([0] object[] parameters)
          IL_0000:  nop
          ------------ new array
          IL_0001:  ldc.i4.2
          IL_0002:  newarr     [mscorlib]System.Object
          IL_0007:  stloc.0
          ------------ value param 0
          IL_0008:  ldloc.0
          IL_0009:  ldc.i4.0
          IL_000a:  ldarg.1
          IL_000b:  box        [mscorlib]System.Int32
          IL_0010:  stelem.ref
          ------------ reference param 1
          IL_0011:  ldloc.0
          IL_0012:  ldc.i4.1
          IL_0013:  ldarg.2
          IL_0014:  stelem.ref
          ------------ 
          IL_0015:  ldarg.0
          IL_0016:  ldfld      class SuperPool.IProxyTypeSink SuperPool.ProxyTest::_sink
          IL_001b:  ldc.i4.s   35
          IL_001d:  ldloc.0
                                                                               object[])
          IL_0026:  nop
          IL_0027:  ret
        */
        /// </summary>
        /// <param name="sinkField">If the sink field is null, we consider it will be the first parameter of the method.</param>
        public static void GenerateProxyMethod(ILGenerator generator, int methodId, FieldBuilder sinkField,
                                               Type[] parametersTypes, Type returnType, MethodInfo receiveMethod, MethodInfo receiveAndReturnMethod)
        {
            LocalBuilder paramsLocal = generator.DeclareLocal(typeof(object[]));

            //generator.EmitWriteLine("1");
            //int parameterStartIndex = 0;
            //if (sinkField == null)
            //{// If the sink field is null, we consider it will be the first parameter of the method.
            //    // So actual parameters start from 1.
            //    parameterStartIndex = 1;
            //}

            if (parametersTypes.Length > 0)
            {
                //generator.Emit(OpCodes.Nop);

                // Load parameter count.
                generator.Emit(OpCodes.Ldc_I4, (Int32)(parametersTypes.Length /*- parameterStartIndex*/));

                // Generate the array and push it to stack.
                generator.Emit(OpCodes.Newarr, typeof(object));

                // Pop the value from the stack and into the params variable.
                generator.Emit(OpCodes.Stloc_0);

                for (int i = 0; i < parametersTypes.Length; i++)
                {
                    // Load the params array.
                    generator.Emit(OpCodes.Ldloc_0);

                    // Push number in array index (int32)
                    generator.Emit(OpCodes.Ldc_I4, (Int32)(i));

                    // Load the first param (uint16)
                    generator.Emit(OpCodes.Ldarg, (UInt16)(i + 1));
                    
                    if (parametersTypes[i].IsClass == false)
                    {// Box the type.
                        generator.Emit(OpCodes.Box, parametersTypes[i]);
                    }

                    // Load the value into the array.
                    generator.Emit(OpCodes.Stelem_Ref);
                }
            }

            if (sinkField == null)
            {// If the sink field is null, we consider it will be the first parameter of the method.
                // Load 0 parameter, that has to be the sink.
                generator.Emit(OpCodes.Ldarg_0);
            }
            else
            {
                // Final section, load this.
                generator.Emit(OpCodes.Ldarg_0);

                // Load the sink field.
                generator.Emit(OpCodes.Ldfld, sinkField);
            }

            if (returnType == typeof(void))
            {
                {// Load parameters
                    // Load the id of the method (int32).
                    generator.Emit(OpCodes.Ldc_I4, (Int32)methodId);

                    // Load the params local.
                    generator.Emit(OpCodes.Ldloc_0);
                }

                generator.EmitCall(OpCodes.Callvirt, receiveMethod, null);
            }
            else
            {
                {// Load parameters.

                    // Load the id of the method (int32).
                    generator.Emit(OpCodes.Ldc_I4, (Int32)methodId);

                    {// Load the return type parameter.
                        generator.Emit(OpCodes.Ldtoken, returnType);
                        // Get the info for the Type.GetTypeFromHandle(), since we invoke it on runtime.
                        MethodInfo typeCallInfo = typeof(Type).GetMethod("GetTypeFromHandle");
                        generator.EmitCall(OpCodes.Call, typeCallInfo, null);
                        //generator.Emit(OpCodes.Ldnull);
                    }

                    // Load the params local.
                    generator.Emit(OpCodes.Ldloc_0);
                }

                // This call will load the result onto the stack, thus making it available for return.
                generator.EmitCall(OpCodes.Callvirt, receiveAndReturnMethod, null);

                if (returnType.IsByRef)
                {// Reference types.
                    if (returnType != typeof(object))
                    {
                        // Need to re-cast.
                        generator.Emit(OpCodes.Castclass, returnType);
                    }
                }
                else
                {// Value types.

                    // Unbox
                    generator.Emit(OpCodes.Unbox_Any, returnType);

                    // Load from stack to variable.
                    //generator.Emit(OpCodes.Stloc_0);
                    //generator.Emit(OpCodes.Brs);
                    //generator.Emit(OpCodes.Ldloc_0);
                }

                //// Pop from stack and load at local variable 0.
                //generator.Emit(OpCodes.Stloc_0);
                //// Load the local variable 0 into stack.
                //generator.Emit(OpCodes.Ldloc_0);
            }

            //generator.Emit(OpCodes.Nop);
            generator.Emit(OpCodes.Ret);
        }

        /// <summary>
        /// Constructor(IProxyTypeSink sink)
        /// {
        ///   this._sink = sink;
        /// }
        /// </summary>
        public static void GenerateConstructor(ILGenerator generator, FieldBuilder proxyInstanceField)
        {
            // For a constructor, argument zero is a reference to the new
            // instance. Push it on the stack before calling the base
            // class constructor. (this is sort of : "this.")
            generator.Emit(OpCodes.Ldarg_0);

            // Specify the default constructor of the base class 
            // (System.Object) by passing an empty array of 
            // types (Type.EmptyTypes) to GetConstructor.
            generator.Emit(OpCodes.Call, typeof(object).GetConstructor(Type.EmptyTypes));

            // Push the instance on the stack.
            generator.Emit(OpCodes.Ldarg_0);

            // Load parameter 1, pushing the argument
            // that is to be assigned to the private field m_number.
            generator.Emit(OpCodes.Ldarg_1);

            generator.Emit(OpCodes.Stfld, proxyInstanceField);
            generator.Emit(OpCodes.Ret);
        }

        /// <summary>
        /// Will create a new one, or if one already in the cache, retrieve that.
        /// </summary>
        /// <typeparam name="TType"></typeparam>
        /// <param name="sink"></param>
        /// <returns></returns>
        public TType ObtainProxyInstance<TType>(IProxyTypeSink sink)
            where TType : class
        {
            Type proxyType = null;
            bool contained = false;
            lock (_typeProxyList)
            {
                contained = _typeProxyList.ContainsKey(typeof(TType));
            }

            if (contained == false)
            {
                GenerateInterfaceProxyImplementation(typeof(TType));
            }

            lock (_typeProxyList)
            {
                proxyType = _typeProxyList[typeof(TType)];
            }

            ConstructorInfo constructorInfo = proxyType.GetConstructor(new Type[] { typeof(IProxyTypeSink) });
            return (TType)constructorInfo.Invoke(new object[] { sink });
        }

        /// <summary>
        /// Obtain a proxy method delegate for a dynamic method.
        /// Obtain only once, when we attach it to the event.
        /// Generate a new dynamic method; we can *NOT REUSE THEM*, since they work on handling events
        /// and each event handler must be traceable to the instance that we subscribed it for.
        /// </summary>
        public GeneratedMethodInfo GenerateDynamicMethodProxyDelegate(Type delegateType)
        {
            GeneratedMethodInfo result = null;

            try
            {
                // Establish delegate parameters.
                MethodInfo delegateMethodInfo = delegateType.GetMethod("Invoke");
                Type[] parameterTypes = ProxyTypeBuilder.GetMethodParametersTypes(delegateMethodInfo);

                List<Type> parameterTypesFull = new List<Type>();
                // First parameter is the sink.
                parameterTypesFull.Add(typeof(IDynamicProxyMethodSink));
                
                // Remaining parameters.
                parameterTypesFull.AddRange(parameterTypes);

                int methodId = PendingDynamicId;

                DynamicMethod standaloneMethod = new DynamicMethod("DynamicMethod_" + methodId.ToString(),
                                                                   delegateMethodInfo.ReturnType, parameterTypesFull.ToArray(), _moduleBuilder);

                result = new GeneratedMethodInfo(methodId, standaloneMethod, delegateType);

                ILGenerator generator = result.StandaloneDynamicMethod.GetILGenerator();

                GenerateProxyMethod(generator, result.Id, null, parameterTypes, delegateMethodInfo.ReturnType, 
                                    _receiveDynamicMethodCall, _receiveDynamicMethodCallAndReturn);

                lock (_methods)
                {// We lock the hot swap, since we also use it to identify items, safer this way.
                    _methods[result.Id] = result;
                }
            }
            catch (Exception ex)
            {
#if Matrix_Diagnostics
                SystemMonitor.OperationError("Failed to generate proxy type for dynamic method.", ex);
#endif
                return null;
            }

            return result;
        }

        ///// <summary>
        ///// Generates a class with a single method inside, with the specified parameters.
        ///// 
        ///// Will generate an "object tag" public item inside the generated type, 
        ///// so this may be used for custom data.
        ///// </summary>
        ///// <returns>Class contains info on the newly generated class and method item.</returns>
        //public GeneratedDynamicMethodTypeInfo GenerateDynamicMethodProxyImplementation(string methodName, 
        //    Type[] parameterTypes, Type returnType)
        //{
        //    GeneratedDynamicMethodTypeInfo result = new GeneratedDynamicMethodTypeInfo();

        //    try
        //    {
        //        lock (this)
        //        {
        //            // This way we are sure no dynamic method with the same name will appear in the current module.
        //            result.TypeId = PendingDynamicId;
        //            // The method bares the same Id as the class that owns it.
        //            int methodId = result.TypeId;

        //            TypeBuilder typeBuilder = _moduleBuilder.DefineType(result.TypeId.ToString() + "_Proxy",
        //                TypeAttributes.Sealed | TypeAttributes.Public);

        //            FieldBuilder sinkField = typeBuilder.DefineField("sink", typeof(IProxyTypeSink), FieldAttributes.Public);
        //            FieldBuilder tagField = typeBuilder.DefineField("tag", typeof(object), FieldAttributes.Public);

        //            // Define a constructor that takes an integer argument and stores it in the private field. 
        //            ConstructorBuilder constructor = typeBuilder.DefineConstructor(MethodAttributes.Public,
        //                CallingConventions.Standard, new Type[] { typeof(IProxyTypeSink) });

        //            GenerateConstructor(constructor.GetILGenerator(), sinkField);

        //            // Generate the required method builder for this method.
        //            MethodBuilder methodBuilder = typeBuilder.DefineMethod(methodName, MethodAttributes.Public,
        //                returnType, parameterTypes);

        //            // Build the method.
        //            GenerateProxyMethod(methodBuilder.GetILGenerator(), methodId, sinkField, parameterTypes, returnType);

        //            result.GeneratedType = typeBuilder.CreateType();

        //            _generatedDynamicMethodTypes[result.TypeId] = result;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        SystemMonitor.OperationError("Failed to generate proxy type for dynamic method.", ex);
        //        return null;
        //    }

        //    return result;
        //}

        /// <summary>
        /// Will generate a proxy implementation of the interface, or return the one stored in the cache.
        /// </summary>
        /// <typeparam name="InterfaceType"></typeparam>
        /// <returns></returns>
        public Type GenerateInterfaceProxyImplementation(Type interfaceType)
        {
            lock (this)
            {// Return from cache.
                try
                {
                    if (_typeProxyList.ContainsKey(interfaceType))
                    {
                        return _typeProxyList[interfaceType];
                    }

                    //Type interfaceType = typeof(InterfaceType);
                    if (interfaceType.IsInterface == false)
                    {
                        throw new InvalidOperationException();
                    }

                    TypeBuilder typeBuilder = _moduleBuilder.DefineType(interfaceType.Name + "_Proxy", 
                                                                        /*TypeAttributes.Sealed |*/ TypeAttributes.Public);

                    typeBuilder.AddInterfaceImplementation(interfaceType);

                    FieldBuilder sinkField = typeBuilder.DefineField("sink", typeof(IProxyTypeSink), FieldAttributes.Public);

                    // Define a constructor that takes an integer argument and stores it in the private field. 
                    ConstructorBuilder constructor = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new Type[] { typeof(IProxyTypeSink) });

                    GenerateConstructor(constructor.GetILGenerator(), sinkField);

                    // Implement methods, this also includes event handlers and property handlers.
                    List<KeyValuePair<int, MethodInfo>> tempMethodData = new List<KeyValuePair<int, MethodInfo>>();
                    MethodInfo[] methodInfos = interfaceType.GetMethods();
                    for (int i = 0; i < methodInfos.Length; i++)
                    {
                        MethodInfo methodInfo = methodInfos[i];
                        MethodBuilder builder = CreateMethodFromMethodInfo(typeBuilder, methodInfo);
                        
                        // Only needed to specify a name change.
                        //typeBuilder.DefineMethodOverride(builder, methodInfo);

                        int methodId = PendingDynamicId;
                        string name = methodInfo.Name;

                        if (methodInfo.IsSpecialName)
                        {// Special name methods are (hopefully only) ones that are generated 
                            // for events "add_" and "remove_" before event name.
                            if (name.StartsWith("add_"))
                            {// Subscribe.
                                GenerateProxyEventMethod(builder.GetILGenerator(), methodId, sinkField, true);
                            }
                            else if (name.StartsWith("remove_"))
                            {// Unsubscribe.
                                GenerateProxyEventMethod(builder.GetILGenerator(), methodId, sinkField, false);
                            }
                            else if (name.StartsWith("get_"))
                            {
                                GenerateProxyPropertyMethod(builder.GetILGenerator(), methodId, methodInfo.ReturnType, sinkField, true);
                            }
                            else if (name.StartsWith("set_"))
                            {
                                ParameterInfo[] parameters = methodInfo.GetParameters();
                                GenerateProxyPropertyMethod(builder.GetILGenerator(), methodId, parameters[0].ParameterType, sinkField, false);
                            }
                            else
                            {// Some strange other special named method has occured, die.
                                throw new Exception("Method specification not recognized.");
                            }
                        }
                        else
                        {// Normal method.

                            GenerateProxyMethod(builder.GetILGenerator(), methodId, sinkField, GetMethodParametersTypes(methodInfo), 
                                                methodInfo.ReturnType, _receiveCallMethodInfo, _receiveCallAndReturnMethodInfo);
                        }

                        tempMethodData.Add(new KeyValuePair<int, MethodInfo>(methodId, methodInfo));
                    }

                    //MethodInfo[] methodInfos = interfaceType.GetMethods();

                    Type proxyType = typeBuilder.CreateType();

                    lock (_methods)
                    {// We lock the hot swap, since we also use it to identify items, safer this way.
                        foreach (KeyValuePair<int, MethodInfo> pair in tempMethodData)
                        {
                            _methods[pair.Key] = new GeneratedMethodInfo(pair.Key, proxyType, pair.Value);
                        }
                    }

                    _typeProxyList[interfaceType] = proxyType;
                    return proxyType;
                }
                catch (Exception ex)
                {
#if Matrix_Diagnostics
                    SystemMonitor.OperationError("Failed to generate proxy type.", ex);
#endif
                    return null;
                }

            }
        }
    }
}
