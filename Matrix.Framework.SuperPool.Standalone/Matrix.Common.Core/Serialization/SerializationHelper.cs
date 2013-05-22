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
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Xml.Serialization;
using System.Xml;
using System.Reflection;
using Matrix.Common.Core;
using Matrix.Common.Core.Collections;
using Matrix.Common.Core.Results;

namespace Matrix.Common.Core.Serialization
{
    /// <summary>
    /// Helps in persisting objects. Provides surrogate selectors for serializing
    /// non serializables like Pen, Brush, etc.
    /// </summary>
    public static class SerializationHelper
    {
        /// <summary>
        /// Any serialization above this limit will produce a warning.
        /// </summary>
        public static int SerializationWarningLimit = 1024 * 1024;

        static IFormatter _formatter = null;
        static object _syncRoot = new object();

        const char MethodSerializationSeparator = '|';

        static BiDictionary<string, MethodBase> _methodSerializationCache = new BiDictionary<string, MethodBase>();


        /// <summary>
        /// Static constructor - create the default formatter.
        /// </summary>
        static SerializationHelper()
        {
            lock (_syncRoot)
            {
                if (_formatter != null)
                {
                    CoreSystemMonitor.Error("Not expected, formatter already created.");
                    return;
                }

                // Create the formatter.
                _formatter = new BinaryFormatter();

                // 2. Construct a SurrogateSelector object
                SurrogateSelector surrogateSelector = new SurrogateSelector();

                // 3. Tell the surrogate selector to use our object when a 
                // object is serialized/deserialized.
                List<ISerializationSurrogate> surrogates = ReflectionHelper.GetTypeChildrenInstances<ISerializationSurrogate>(
                    ReflectionHelper.GetAssemblies(true, true));

                foreach (ISerializationSurrogate surrogate in surrogates)
                {
                    SerializationSurrogateAttribute attribute = ReflectionHelper.GetTypeCustomAttributeInstance<SerializationSurrogateAttribute>(
                        surrogate.GetType(), false);

                    if (attribute != null)
                    {
                        surrogateSelector.AddSurrogate(attribute.Type, new StreamingContext(StreamingContextStates.All), surrogate);
                    }
                    else
                    {
                        CoreSystemMonitor.Info(string.Format("Surrogate type [{0}] not marked with SerializationSurrogateAttribute.", surrogate.GetType().Name));
                    }
                }

                _formatter.SurrogateSelector = surrogateSelector;
            }
        }

        public static IFormatter ObtainFormatter()
        {
            return _formatter;
        }

        /// <summary>
        /// Will clone the object using binary serialization.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static object BinaryClone(object input)
        {
            byte[] bytes = Serialize(input);
            object result = null;
            if (Deserialize(bytes, out result).IsFailure)
            {
                return null;
            }

            return result;
        }

        /// <summary>
        /// Convert Method information to string representation.
        /// </summary>
        /// <param name="method"></param>
        /// <returns></returns>
        public static string SerializeMethodBaseToString(MethodBase method, bool useCache)
        {
            string result = null;
            if (useCache)
            {
                lock (_methodSerializationCache)
                {
                    if (_methodSerializationCache.TryGetByValue(method, ref result))
                    {
                        return result;
                    }
                }
            }

            StringBuilder valueBuilder = new StringBuilder();
            valueBuilder.Append(method.DeclaringType.AssemblyQualifiedName);
            valueBuilder.Append(MethodSerializationSeparator);
            valueBuilder.Append(method.Name);

            foreach (ParameterInfo parameter in method.GetParameters())
            {// Add parameters types.
                valueBuilder.Append(MethodSerializationSeparator);
                valueBuilder.Append(parameter.ParameterType.AssemblyQualifiedName);
            }

            result = valueBuilder.ToString();
            if (useCache)
            {
                lock (_methodSerializationCache)
                {// Possible multiple entry, but not a problem, also faster.
                    _methodSerializationCache.Add(result, method);
                }
            }

            return result;
        }

        /// <summary>
        /// Convert Method information from its string representation.
        /// </summary>
        /// <param name="methodInfo"></param>
        /// <returns></returns>
        public static MethodInfo DeserializeMethodBaseFromString(string methodInfoName, bool useCache)
        {
            if (useCache)
            {
                lock (_methodSerializationCache)
                {
                    MethodBase tmpResult = null;
                    if (_methodSerializationCache.TryGetByKey(methodInfoName, ref tmpResult))
                    {
                        return (MethodInfo)tmpResult;
                    }
                }
            }

            string[] values = methodInfoName.Split(MethodSerializationSeparator);
            if (values.Length < 2)
            {
                return null;
            }

            Type declaringType = Type.GetType(values[0]);
            string methodName = values[1];

            Type[] argumentsTypes = new Type[values.Length - 2];
            for (int i = 2; i < values.Length; i++)
            {
                argumentsTypes[i - 2] = Type.GetType(values[i]);
            }


            MethodInfo result = null;

            
            // *IMPORTANT*
            // We need to do this, in order to be able to obtain private methods too.

            MethodInfo[] methods= declaringType.GetMethods(BindingFlags.Default | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            foreach (MethodInfo potentialMethodInfo in methods)
            {
                if (potentialMethodInfo.Name == methodName)
                {// Same name.
                    ParameterInfo[] parameters = potentialMethodInfo.GetParameters();
                    if (parameters.Length == argumentsTypes.Length)
                    {
                        bool parameterMatch = true;
                        for (int i = 0; i < parameters.Length; i++)
                        {
                            if (parameters[i].ParameterType != argumentsTypes[i])
                            {
                                parameterMatch = false;
                                break;
                            }
                        }

                        if (parameterMatch)
                        {
                            result = potentialMethodInfo;
                        }
                    }
                }

                if (result != null)
                {
                    break;
                }
            }

            if (useCache && result != null)
            {
                lock (_methodSerializationCache)
                {// Possible multiple entry, but not a problem, also faster.
                    _methodSerializationCache.Add(methodInfoName, result);
                }
            }
            
            return result;
        }

        /// <summary>
        /// Perform object serialization to a file.
        /// </summary>
        public static Result Serialize(string filename, object value)
        {
            try
            {
                // Create folder if not exists.
                if (Directory.Exists(Path.GetDirectoryName(filename)) == false)
                {
                    DirectoryInfo info = Directory.CreateDirectory(Path.GetDirectoryName(filename));
                    if (info == null || info.Exists == false)
                    {
                        return Result.Fail(string.Format("Failed to create directory [{0}].", Path.GetDirectoryName(filename)));
                    }
                }

                byte[] data = SerializationHelper.Serialize(value);
                File.WriteAllBytes(filename, data);
            }
            catch (Exception ex)
            {
                string message = string.Format("Failed to serialize to file [{0}]", filename);
                CoreSystemMonitor.OperationError(message, ex);
                return Result.Fail(message);
            }

            return Result.Success;
        }


        /// <summary>
        /// Perform object serialization to a array of bytes.
        /// </summary>
        public static byte[] Serialize(object p)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                Serialize(ms, p);
                ms.Flush();
                return ms.GetBuffer();
            }
        }

        /// <summary>
        /// Helper, overrides.
        /// </summary>
        public static Result Deserialize(byte[] data, out object value)
        {
            using (MemoryStream ms = new MemoryStream(data))
            {
                return Deserialize(ms, out value);
            }
        }

        /// <summary>
        /// Helper, overrides.
        /// </summary>
        public static TType Deserialize<TType>(string filename)
            where TType : class
        {
            try
            {
                byte[] data = File.ReadAllBytes(filename);
                if (data != null)
                {
                    object dummy;
                    SerializationHelper.Deserialize(data, out dummy);
                    return (TType)dummy;
                }
            }
            catch (Exception ex)
            {
                CoreSystemMonitor.OperationError("Failed to extract proxies manager.", ex);
            }

            return null;
        }

        /// <summary>
        /// Perform deserialization of an object from a stream.
        /// </summary>
        public static Result Deserialize(MemoryStream stream, out object value)
        {
            try
            {
                IFormatter formatter = ObtainFormatter();
                value = formatter.Deserialize(stream);
            }
            catch (Exception ex)
            {
                CoreSystemMonitor.Error("Failed to deserialize object.", ex);
                value = null;
                return Result.Fail(ex);
            }

            return Result.Success;
        }

        /// <summary>
        /// Serialize to memory stream.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="p"></param>
        /// <returns></returns>
        public static Result Serialize(MemoryStream stream, object p)
        {
            try
            {
                IFormatter formatter = ObtainFormatter();
                formatter.Serialize(stream, p);
                if (stream.Position > SerializationWarningLimit)
                {
                    CoreSystemMonitor.Warning("Serialialization of object [" + p.GetType().Name + "] has grown above the default serialization limit to [" + stream.Position.ToString() + "] bytes.");
                }

                return Result.Success;
            }
            catch (Exception ex)
            {
                CoreSystemMonitor.Error("Failed to serialize object [" + p.GetType().Name + "," + ex.Message + "].");
                return Result.Fail(ex);
            }
        }

        /// <summary>
        /// Serialize to file.
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="p"></param>
        /// <returns></returns>
        public static Result SerializeXml(string filename, object p)
        {
            try
            {
                // Create folder if not exists.
                if (Directory.Exists(Path.GetDirectoryName(filename)) == false)
                {
                    DirectoryInfo info = Directory.CreateDirectory(Path.GetDirectoryName(filename));
                    if (info == null || info.Exists == false)
                    {
                        return Result.Fail(string.Format("Failed to create directory [{0}].", Path.GetDirectoryName(filename)));
                    }
                }

                using (XmlWriter xw = XmlWriter.Create(filename))
                {
                    return SerializationHelper.SerializeXml(xw, p);
                }
            }
            catch (Exception ex)
            {
                CoreSystemMonitor.OperationError("Failed to serialize object [" + p.ToString() + "] to xml file [" + filename + "].", ex);
                return Result.Failure;
            }
        }

        /// <summary>
        /// Serialize to string builder.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="p"></param>
        /// <returns></returns>
        public static Result SerializeXml(StringBuilder builder, object p)
        {
            try
            {
                using (XmlWriter xw = XmlWriter.Create(builder))
                {
                    return SerializationHelper.SerializeXml(xw, p);
                }
            }
            catch (Exception ex)
            {
                CoreSystemMonitor.OperationError("Failed to serialize object [" + p.ToString() + "] to xml, string builder.", ex);
                return Result.Fail(ex);
            }
        }

        /// <summary>
        /// Serialize to memory stream.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="p"></param>
        /// <returns></returns>
        public static Result SerializeXml(MemoryStream stream, object p)
        {
            try
            {
                using (XmlWriter xw = XmlWriter.Create(stream))
                {
                    return SerializationHelper.SerializeXml(xw, p);
                }
            }
            catch (Exception ex)
            {
                CoreSystemMonitor.OperationError("Failed to serialize object [" + p.ToString() + "] to xml stream.", ex);
                return Result.Fail(ex);
            }
        }

        /// <summary>
        /// Serialize to xml writer.
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="p"></param>
        /// <returns></returns>
        public static Result SerializeXml(XmlWriter writer, object p)
        {
            try
            {
                XmlSerializer serializer = new XmlSerializer(p.GetType());
                serializer.Serialize(writer, p, null);
            }
            catch (Exception ex)
            {
                CoreSystemMonitor.Error("Failed to xml-serialize object [" + p.GetType().Name + "]", ex);
                return Result.Fail(ex);
            }

            return Result.Success;
        }

        /// <summary>
        /// Serialize to text writer.
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="p"></param>
        /// <returns></returns>
        public static Result SerializeXml(TextWriter writer, object p)
        {
            try
            {
                XmlSerializer serializer = new XmlSerializer(p.GetType());
                serializer.Serialize(writer, p);
            }
            catch (Exception ex)
            {
                CoreSystemMonitor.Error("Failed to xml-serialize object [" + p.GetType().Name + "]", ex);
                return Result.Fail(ex);
            }

            return Result.Success;
        }

        public static Result DeSerializeXml<TType>(string file, out TType output)
            where TType : class
        {
            try
            {
                using (XmlReader reader = XmlTextReader.Create(file))
                {
                    return SerializationHelper.DeSerializeXml<TType>(reader, out output);
                }
            }
            catch (Exception ex)
            {
                output = null;
                return Result.Fail(ex);
            }
        }

        public static Result DeSerializeXml<TType>(XmlReader reader, out TType output)
            where TType : class
        {
            output = null;
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(TType));
                output = (TType)serializer.Deserialize(reader);
            }
            catch (Exception ex)
            {
                CoreSystemMonitor.Error("Failed to xml-deserialize object [" + typeof(TType).Name + "]", ex);
                return Result.Fail(ex);
            }

            return Result.Success;
        }

        public static Result DeSerializeXml<TType>(MemoryStream reader, out TType output)
            where TType : class
        {
            output = null;
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(TType));
                output = (TType)serializer.Deserialize(reader);
            }
            catch (Exception ex)
            {
                CoreSystemMonitor.Error("Failed to xml-deserialize object [" + typeof(TType).Name + "]", ex);
                return Result.Fail(ex);
            }

            return Result.Success;
        }

        public static Result DeSerializeXml<TType>(TextReader reader, out TType output)
            where TType : class
        {
            output = null;
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(TType));
                output = (TType)serializer.Deserialize(reader);
            }
            catch (Exception ex)
            {
                CoreSystemMonitor.Error("Failed to xml-deserialize object [" + typeof(TType).Name + "]", ex);
                return Result.Fail(ex);
            }

            return Result.Success;
        }

        ///// <summary>
        ///// Special helper for doing data from a SerializationInfo.
        ///// </summary>
        //public static void SerializeInfo(Stream stream, SerializationInfo info)
        //{
        //    try
        //    {
        //        IFormatter formatter = GenerateFormatter();

        //        SerializationInfoEnumerator enumerator = info.GetEnumerator();
        //        while (enumerator.MoveNext())
        //        {
        //            formatter.SaveState(stream, enumerator.Current.Name);
        //            formatter.SaveState(stream, enumerator.Current.Value);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        SystemMonitor.Error("Failed to serialize info [" + ex.Message + "].");
        //    }
        //}

        ///// <summary>
        ///// Special helper for doing data from a SerializationInfo.
        ///// </summary>
        //public static void DeSerializeInfo(SerializationInfo info, Stream stream)
        //{
        //    try
        //    {
        //        IFormatter formatter = GenerateFormatter();

        //        string name;
        //        do
        //        {
        //            name = (string)formatter.Deserialize(stream);
        //            object value = formatter.Deserialize(stream);
        //            info.AddValue(name, value);
        //        }
        //        while (string.IsNullOrEmpty(name) == false);
        //    }
        //    catch (Exception ex)
        //    {
        //        SystemMonitor.Error("Failed to serialize info [" + ex.Message + "].");
        //    }
        //}
    }
}
