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
using System.IO;
using System.Diagnostics;
using Matrix.Common.Core.Collections;

namespace Matrix.Common.Core
{
    /// <summary>
    /// Helper static class handling reflection operations.
    /// Make sure to use it extensively, since it contains some dynamic runtime reflection references as well.
    /// </summary>
    public static class ReflectionHelper
    {
        static Dictionary<Assembly, List<Assembly>> _dynamicReferencedAssemblies = new Dictionary<Assembly, List<Assembly>>();

        /// <summary>
        /// Static constructor.
        /// </summary>
        static ReflectionHelper()
        {
            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(CurrentDomain_AssemblyResolve);
        }

        /// <summary>
        /// Domain manager has failed to find an assembly (probably a dynamically loaded one) so help him.
        /// Although the assembly is loaded, the manager fails to find it, since its a twat.
        /// </summary>
        static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (Assembly assembly in assemblies)
            {
                if (assembly.GetName().FullName == args.Name)
                {
                    return assembly;
                }
            }
            return null;
        }

        /// <summary>
        /// Add a dynamicly referenced assembly.
        /// </summary>
        public static void AddDynamicReferencedAssembly(Assembly sourceAssembly, Assembly referencedAssembly)
        {
            lock (_dynamicReferencedAssemblies)
            {
                if (_dynamicReferencedAssemblies.ContainsKey(sourceAssembly) == false)
                {
                    _dynamicReferencedAssemblies.Add(sourceAssembly, new List<Assembly>());
                }

                if (_dynamicReferencedAssemblies[sourceAssembly].Contains(referencedAssembly) == false)
                {
                    _dynamicReferencedAssemblies[sourceAssembly].Add(referencedAssembly);
                }
            }
        }

        static public MethodInfo GetMethodInfo(CommonHelper.DefaultDelegate delegateInstance)
        {
            return delegateInstance.Method;
        }

        static public MethodInfo GetMethodInfo<TParam1>(CommonHelper.GenericDelegate<TParam1> delegateInstance)
        {
            return delegateInstance.Method;
        }

        static public MethodInfo GetMethodInfo<TParam1, TParam2>(CommonHelper.GenericDelegate<TParam1, TParam2> delegateInstance)
        {
            return delegateInstance.Method;
        }

        static public MethodInfo GetMethodInfo<TParam1, TParam2, TParam3>(CommonHelper.GenericDelegate<TParam1, TParam2, TParam3> delegateInstance)
        {
            return delegateInstance.Method;
        }

        /// <summary>
        /// Will return all the properties and methods of a given type, 
        /// that have the designated return type and take no parameter. Used in automated statistics.
        /// </summary>
        /// <param name="individualType"></param>
        /// <returns></returns>
        static public MethodInfo[] GetTypePropertiesAndMethodsByReturnType(Type objectType, Type[] returnTypes)
        {
            List<MethodInfo> resultList = new List<MethodInfo>();

            MethodInfo[] allMethods = objectType.GetMethods(BindingFlags.Public | BindingFlags.Instance);

            foreach (MethodInfo methodInfo in allMethods)
            {
                bool doesMatchReturnType = false;
                foreach (Type type in returnTypes)
                {
                    if (methodInfo.ReturnType == type)
                    {
                        doesMatchReturnType = true;
                        break;
                    }
                }

                if (methodInfo.GetParameters().Length == 0 && doesMatchReturnType)
                {// No params, proper return type - this is the one.
                    resultList.Add(methodInfo);
                }
            }

            MethodInfo[] resultArray = new MethodInfo[resultList.Count];
            resultList.CopyTo(resultArray);
            return resultArray;
        }

        /// <summary>
        /// Will create for you the needed instances of the given type children types
        /// with DEFAULT CONSTRUCTORS only, no params.
        /// </summary>
        static public List<TypeRequired> GetTypeChildrenInstances<TypeRequired>(System.Reflection.Assembly assembly)
        {
            return GetTypeChildrenInstances<TypeRequired>(new Assembly[] { assembly });
        }

        /// <summary>
        /// Get the instance of the custom attribute on this type (or null if none).
        /// </summary>
        static public AttributeType GetTypeCustomAttributeInstance<AttributeType>(Type type, bool inherit)
            where AttributeType : class
        {
            object[] attributes = type.GetCustomAttributes(inherit);
            foreach (object attribute in attributes)
            {
                if (attribute.GetType() == typeof(AttributeType))
                {
                    return (AttributeType) attribute;
                }
            }

            return null;
        }

        /// <summary>
        /// Helper, establish if the given type is marked with this custom attribute.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="attributeType"></param>
        /// <returns></returns>
        static public bool TypeHasCustomAttribute(Type type, Type attributeType, bool inherit)
        {
            object[] attributes = type.GetCustomAttributes(attributeType, inherit);
            return attributes != null && attributes.Length > 0;
        }

        /// <summary>
        /// Will create for you the needed instances of the given type children types
        /// with DEFAULT CONSTRUCTORS only, no params.
        /// </summary>
        static public List<TypeRequired> GetTypeChildrenInstances<TypeRequired>(IEnumerable<Assembly> assemblies)
        {
            List<TypeRequired> resultingInstances = new List<TypeRequired>();

            foreach (Assembly assembly in assemblies)
            {
                // Collect all the types that match the description
                List<Type> blockTypes = ReflectionHelper.GetTypeChildrenTypes(typeof(TypeRequired), assembly);

                foreach (Type blockType in blockTypes)
                {
                    System.Reflection.ConstructorInfo[] constructorInfo = blockType.GetConstructors();

                    if (constructorInfo == null || constructorInfo.Length == 0 ||
                        blockType.IsAbstract || blockType.IsClass == false)
                    {
                        continue;
                    }

                    resultingInstances.Add((TypeRequired)constructorInfo[0].Invoke(null));
                }
            }

            return resultingInstances;
        }

        //static public int GetEnumValueIndex(System.Type enumType, string valueName)
        //{
        //    string[] names = System.Enum.GetNames(enumType);
        //    for (int i = 0; i < names.Length; i++)
        //    {
        //        if (names[i] == valueName)
        //        {
        //            return i;
        //        }
        //    }
        //    throw new Exception("Invalid enum value name passed in.");
        //}

        /// <summary>
        /// Loads all the assemblies in the directory of the executing (entry) assembly and searches 
        /// them for inheritors of the given type. SLOW!
        /// </summary>
        /// <returns></returns>
        //static public List<Type> GetCollectTypeChildrenTypesFromRelatedAssemblies(Type typeSearched)
        //{
        //    List<Type> result = new List<Type>();

        //    // Load all the assemblies in the directory of the current application and try to find
        //    // inheritors of AIndividual in them, then gather those in the list.
        //    string path = Assembly.GetEntryAssembly().Location;
        //    path = path.Remove(path.LastIndexOf('\\'));
        //    string[] dllFiles = System.IO.Directory.GetFiles(path, "*.dll");

        //    foreach (string file in dllFiles)
        //    {
        //        Assembly assembly;
        //        try
        //        {
        //            assembly = Assembly.LoadFile(file);
        //        }
        //        catch (Exception)
        //        {// This DLL was not a proper assembly, disregard.
        //            continue;
        //        }
        //        // Try to find typeSearched inheritors in this assembly.
        //        result.AddRange(ReflectionSupport.GetTypeChildrenTypes(typeSearched, assembly));
        //    }
        //    return result;
        //}

        /// <summary>
        /// Helper method allows to retrieve application entry assembly referenced (static and runtime) assemblies.
        /// </summary>
        static public List<Assembly> GetAssemblies(bool entryAssembly, bool entryReferencedAssemblies)
        {
            Assembly startAssembly = Assembly.GetEntryAssembly();
            if (startAssembly == null)
            {
                startAssembly = Assembly.GetCallingAssembly();
                CoreSystemMonitor.OperationWarning("Failed to find application entry assembly, operating with reduced set.");
            }

            return GetReferencedAndInitialAssembly(startAssembly);
        }

        ///// <summary>
        ///// Helper method allows to retrieve application entry assembly as well as its referenced (static and runtime) assemblies.
        ///// </summary>
        //static public List<Assembly> GetApplicationEntryAssemblyAndReferencedAssemblies()
        //{
        //}

        /// <summary>
        /// Helper method allows to retrieve initial assembly and it referenced (static and runtime) assemblies.
        /// </summary>
        static public ListUnique<Assembly> GetReferencedAndInitialAssembly(Assembly initialAssembly)
        {
            ListUnique<Assembly> assemblies = GetReferencedAssemblies(initialAssembly);
            assemblies.Add(initialAssembly);
            return assemblies;
        }

        /// <summary>
        /// Helper method allows to retrieve initial assembly referenced (static and runtime) assemblies.
        /// </summary>
        static public ListUnique<Assembly> GetReferencedAssemblies(Assembly initialAssembly)
        {
            ListUnique<Assembly> result = new ListUnique<Assembly>();

            AssemblyName[] names = initialAssembly.GetReferencedAssemblies();
            for (int i = 0; i < names.Length; i++)
            {
                result.Add(Assembly.Load(names[i]));
            }

            lock(_dynamicReferencedAssemblies)
            {
                if (_dynamicReferencedAssemblies.ContainsKey(initialAssembly))
                {
                    result.AddRange(_dynamicReferencedAssemblies[initialAssembly]);
                }
            }

            return result;
        }

        /// <summary>
        /// Helper method, allows to retrieve a list of children types to the parent type, from list of referenced assemblies.
        /// </summary>
        static public List<Type> GatherTypeChildrenTypesFromAssemblies(Type parentType, IEnumerable<Assembly> assemblies)
        {
            return GatherTypeChildrenTypesFromAssemblies(parentType, assemblies, false, true, null);
        }

        /// <summary>
        /// This will look for children types in Entry, Current, Executing, Calling assemly, 
        /// as well as assemblies with names specified and found in the directory of the current application.
        /// </summary>
        /// <param name="mandatoryAttributesType">A list of attributes the class must have, or null for no requirement.</param>
        /// <returns></returns>
        static public List<Type> GatherTypeChildrenTypesFromAssemblies(Type parentType, IEnumerable<Assembly> assemblies, 
                                                                       bool allowOnlyClasses, bool allowAbstracts, Type[] mandatoryAttributesType)
        {
            List<Type> resultingTypes = new List<Type>();
            if (assemblies == null)
            {
                return resultingTypes;
            }

            foreach (Assembly assembly in assemblies)
            {
                List<Type> types = ReflectionHelper.GetTypeChildrenTypes(parentType, assembly);
                foreach (Type type in types)
                {
                    if ((allowOnlyClasses == false || type.IsClass)
                        && (allowAbstracts || type.IsAbstract == false))
                    {
                        if (mandatoryAttributesType != null && mandatoryAttributesType.Length > 0)
                        {
                            bool attributesFound = true;
                            foreach (Type attributeType in mandatoryAttributesType)
                            {
                                if (TypeHasCustomAttribute(type, attributeType, true) == false)
                                {
                                    attributesFound = false;
                                    break;
                                }
                            }
                            if (attributesFound == false)
                            {// Type not comply with attribute requirements.
                                continue;
                            }
                        }

                        resultingTypes.Add(type);
                    }
                }
            }

            return resultingTypes;
        }

        /// <summary>
        /// Extended baseMethod, allows to specify the requrired constructor parameters.
        /// </summary>
        static public List<Type> GatherTypeChildrenTypesFromAssemblies(Type parentType, bool exactParameterTypeMatch, 
                                                                       bool allowAbstract, IEnumerable<Assembly> assemblies, Type[] constructorParametersTypes)
        {
            List<Type> candidateTypes = GatherTypeChildrenTypesFromAssemblies(parentType, assemblies);
            List<Type> resultingTypes = new List<Type>();

            if (constructorParametersTypes == null)
            {
                constructorParametersTypes = new Type[] { };
            }

            foreach (Type type in candidateTypes)
            {
                ConstructorInfo constructorInfo = type.GetConstructor(constructorParametersTypes);
                if (constructorInfo != null)
                {
                    bool isValid = true;
                    if (allowAbstract == false)
                    {
                        isValid = type.IsAbstract == false && type.IsClass == true;
                    }

                    if (isValid && exactParameterTypeMatch)
                    {// Perform check for exact type match, evade parent classes.

                        ParameterInfo[] parameterInfos = constructorInfo.GetParameters();
                        for (int i = 0; i < parameterInfos.Length; i++)
                        {
                            if (parameterInfos[i].ParameterType != constructorParametersTypes[i])
                            {// Not good, skip.
                                isValid = false;
                                break;
                            }
                        }
                    }

                    if (isValid)
                    {
                        resultingTypes.Add(type);
                    }
                }
            }

            return resultingTypes;
        }

        /// <summary>
        /// SLOW, see GetCallingMethod(); provides the original calling method outside the owner type
        /// (if such a method exists in the call stack).
        /// </summary>
        /// <param name="stackPop"></param>
        /// <returns></returns>
        public static MethodBase GetExternalCallingMethod(int stackLoopback, List<Type> ownerTypesIgnored)
        {
            MethodBase baseMethod = ReflectionHelper.GetCallingMethod(stackLoopback);
            MethodBase method = baseMethod;
            
            int methodIndex = stackLoopback + 1;
            if (ownerTypesIgnored != null)
            {
                while (ownerTypesIgnored.Contains(method.DeclaringType))
                {// All calls from system monitor get traced back one additional step backwards.

                    method = ReflectionHelper.GetCallingMethod(methodIndex);
                    methodIndex++;

                    if (method == null)
                    {
                        method = baseMethod;
                        break;
                    }
                }
            }

            return method;
        }

        /// <summary>
        /// May return emtpy, or some - depending on if the types are known or not.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static List<Type> GetKnownTypes(List<string> names)
        {
            List<Type> result = new List<Type>();
            foreach (string name in names)
            {
                Type type = Type.GetType(name, false);
                if (type != null)
                {
                    result.Add(type);
                }
            }
            return result;
        }

        /// <summary>
        /// Obtain the assembly qualified name of the type, as well as all the names
        /// of the types (incl. interfaces) that it inherits.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static List<string> GetTypeNameAndRelatedTypes(Type type)
        {
            if (type == null)
            {
                return null;
            }

            List<string> result = new List<string>();
            result.Add(type.AssemblyQualifiedName);

            Type superType = type.BaseType;
            while(superType != null
                  && superType.BaseType != typeof(object)
                  && superType.BaseType != null)
            {
                result.Add(superType.AssemblyQualifiedName);
                superType = superType.BaseType;
            }

            foreach (Type interfaceType in type.GetInterfaces())
            {
                result.Add(interfaceType.AssemblyQualifiedName);
            }

            return result;
        }

        /// <summary>
        /// SLOW, retrieves information for a calling baseMethod using the stack.
        /// </summary>
        /// <param name="stackPop">How many steps up the stack to take, before providing the baseMethod info.</param>
        public static MethodBase GetCallingMethod(int stackPop)
        {
            StackTrace _callStack = new StackTrace(); // The call stack  
            if (stackPop >= _callStack.FrameCount)
            {
                return null;
            }

            StackFrame frame = _callStack.GetFrame(stackPop); // The frame that called me.
            return frame.GetMethod(); // The baseMethod that called me.
        }

        /// <summary>
        /// SLOW, retrieves full name for a calling baseMethod using the stack.
        /// </summary>
        /// <param name="stackPop">How many steps up the stack to take, before providing the baseMethod info.</param>
        public static string GetFullCallingMethodName(int stackPop)
        {
            StackTrace _callStack = new StackTrace(); // The call stack  
            StackFrame frame = _callStack.GetFrame(stackPop); // The frame that called me
            MethodBase method = frame.GetMethod(); // The baseMethod that called me

            string assemblyName = method.DeclaringType.Assembly.GetName().Name;

            return assemblyName + "." + method.DeclaringType.Name + "." + method.Name;
        }

        /// <summary>
        /// Obtain a full name of the method, incl. its parameters types,
        /// so the method can be uniquely identified in a type.
        /// </summary>
        /// <param name="method"></param>
        /// <returns></returns>
        public static string GetDetailedMethodName(MethodBase method)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(method.Name);
            builder.Append("(");
            foreach (ParameterInfo parameter in method.GetParameters())
            {
                builder.Append(parameter.ParameterType.Name);
            }
            builder.Append(")");
            return builder.ToString();
        }

        /// <summary>
        /// Collect them from a given assembly. Also works for interfaces.
        /// </summary>
        static public List<Type> GetTypeChildrenTypes(Type typeSearched, System.Reflection.Assembly assembly)
        {
            List<Type> result = new List<Type>();
            Type[] types;

            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                string message = string.Empty;
                foreach (Exception subEx in ex.LoaderExceptions)
                {
                    message += "{" + CommonHelper.GetExceptionMessage(subEx) + "}";
                }

                CoreSystemMonitor.OperationError("Failed to load assembly types for [" + typeSearched.Name + ", " + assembly.GetName().Name + "] [" + CommonHelper.GetExceptionMessage(ex) + ", " + message + "].");
                return result;
            }
            catch (Exception ex)
            {
                CoreSystemMonitor.OperationError("Failed to load assembly types for [" + typeSearched.Name + ", " + assembly.GetName().Name + "] [" + CommonHelper.GetExceptionMessage(ex) + "].");
                return result;
            }

            foreach (Type type in types)
            {
                if (typeSearched.IsInterface)
                {
                    List<Type> interfaces = new List<Type>(type.GetInterfaces());
                    if (interfaces.Contains(typeSearched))
                    {
                        result.Add(type);
                    }
                }
                else if (type.IsSubclassOf(typeSearched))
                {
                    result.Add(type);
                }
            }
            return result;
        }

        /// <summary>
        /// Collect all the types of the interfaces.
        /// </summary>
        /// <param name="objectType"></param>
        /// <param name="attributeType"></param>
        /// <returns></returns>
        static public IEnumerable<Type> GatherTypeAttributeMarkedInterfaces(Type objectType, Type attributeType)
        {
            foreach(Type interfaceType in objectType.GetInterfaces())
            {
                if (ReflectionHelper.TypeHasCustomAttribute(interfaceType, attributeType, false))
                {
                    yield return interfaceType;
                }
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <param name="attributeType"></param>
        /// <param name="bindingFlags">For ex. : BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance</param>
        /// <param name="checkParents"></param>
        /// <returns></returns>
        static public List<MethodInfo> GatherTypeMethodsByAttribute(Type inputType, Type attributeType, 
                                                                    BindingFlags bindingFlags, bool processParentTypes)
        {
            List<MethodInfo> result = new List<MethodInfo>();

            Type currentType = inputType;

            while (currentType != typeof(object))
            {// Gather current type members, but also gather parents private types, since those will not be available to the child class and will be missed.
                // Also not that the dictionary key mechanism throws if same baseMethod is entered twise - so it is a added safety feature.

                foreach (MethodInfo methodInfo in currentType.GetMethods())
                {
                    object[] customAttributes = methodInfo.GetCustomAttributes(false);

                    if (currentType != inputType && methodInfo.IsPrivate == false)
                    {// Since this is one of the members of the parent classes, make sure to just gather privates.
                        // because all of the parent's protected and public methods are available from the child class.
                        continue;
                    }

                    foreach (object attribute in customAttributes)
                    {
                        if (attribute.GetType() == attributeType)
                        {
                            if (result.Contains(methodInfo) == false)
                            {
                                result.Add(methodInfo);
                            }
                        }
                    }
                }

                currentType = currentType.BaseType;
                if (processParentTypes == false)
                {
                    break;
                }
            }

            return result;
        }

        /// <summary>
        /// Does the type implement the specified interface type.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="interfaceType"></param>
        /// <returns></returns>
        public static bool IsTypeImplementingInterface(Type type, Type interfaceType)
        {
            foreach (Type implementedInterfaceType in type.GetInterfaces())
            {
                if (implementedInterfaceType == interfaceType)
                {
                    return true;
                }
            }

            return false;
        }

    }
}
