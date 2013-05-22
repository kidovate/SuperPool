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

namespace Matrix.Common.Core.Serialization
{
    /// <summary>
    /// Helper class to contain and make easier access to serialization data of all serializable types.
    /// Extended version of the System SerializationInfo class.
    /// </summary>
    [Serializable]
    public class SerializationInfoEx
    {
        readonly Dictionary<string, object> _objects = new Dictionary<string, object>();

        public object this[string name]
        {
            get
            {
                lock (this)
                {
                    object value;
                    if (_objects.TryGetValue(name, out value))
                    {
                        return value;
                    }

                    return null;
                }
            }

            set
            {
                lock (this)
                {
                    _objects[name] = value;
                }
            }

        }

        /// <summary>
        /// Default constructor.
        /// </summary>
        public SerializationInfoEx()
        {
        }

        /// <summary>
        /// Deserialize data from stream.
        /// </summary>
        public SerializationInfoEx(MemoryStream stream)
        {
            object value;
            if (SerializationHelper.Deserialize(stream, out value).IsSuccess)
            {
                _objects = (Dictionary<string, object>)value;
            }
        }

        /// <summary>
        /// Clear persisted data.
        /// </summary>
        public void Clear()
        {
            lock (this)
            {
                _objects.Clear();
            }
        }

        /// <summary>
        /// Clear persisted data by part (or all) of name.
        /// </summary>
        public void ClearByNamePart(string namePart)
        {
            lock (this)
            {
                foreach (string pairName in CommonHelper.EnumerableToArray(_objects.Keys))
                {
                    if (pairName.Contains(namePart))
                    {
                        _objects.Remove(pairName);
                    }
                }
            }
        }

        /// <summary>
        /// Delete the value with the given name.
        /// </summary>
        /// <param name="name"></param>
        public bool DeleteValue(string name)
        {
            lock (this)
            {
                return _objects.Remove(name);
            }
        }

        /// <summary>
        /// Convert persisted data to stream.
        /// </summary>
        /// <returns></returns>
        public MemoryStream ToStream()
        {
            lock (this)
            {
                MemoryStream stream = new MemoryStream();
                SerializationHelper.Serialize(stream, _objects);
                return stream;
            }
        }

        /// <summary>
        /// Add new value to the list of persisted values.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public void AddValue(string name, object value)
        {
            lock (this)
            {
                _objects[name] = value;
            }
        }

        /// <summary>
        /// Read string value.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public string GetString(string name)
        {
            return GetValue<string>(name);
        }

        /// <summary>
        /// Read an int value.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public int GetInt32(string name)
        {
            return GetValue<Int32>(name);
        }

        /// <summary>
        /// Read a single value.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public Single GetSingle(string name)
        {
            return GetValue<Single>(name);
        }

        /// <summary>
        /// Read a boolean value.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool GetBoolean(string name)
        {
            return GetValue<bool>(name);
        }

        /// <summary>
        /// Obtain a serialized value with the type T and name.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <returns></returns>
        public T GetValue<T>(string name)
        {
            lock (this)
            {
                return (T)_objects[name];
            }
        }
        
        /// <summary>
        /// 
        /// </summary>
        public bool TryGetValue<T>(string name, ref T value)
        {
            lock (this)
            {
                if (_objects.ContainsKey(name))
                {
                    value = (T)_objects[name];
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// If value is not present, create a new instance and return it.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <returns></returns>
        public T GetValueOrNew<T>(string name)
            where T : new()
        {
            lock (this)
            {
                if (ContainsValue(name))
                {
                    return (T)_objects[name];
                }
                else
                {
                    return new T();
                }
            }
        }

        /// <summary>
        /// Helper, redefine of ContainsValue().
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool HasValue(string name)
        {
            return ContainsValue(name);
        }

        /// <summary>
        /// Check if a value with this name is contained in the list of serialized values.
        /// </summary>
        public bool ContainsValue(string name)
        {
            lock (this)
            {
                return _objects.ContainsKey(name);
            }
        }
    }
}
