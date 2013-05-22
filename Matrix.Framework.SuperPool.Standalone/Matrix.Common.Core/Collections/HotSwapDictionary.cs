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

namespace Matrix.Common.Core.Collections
{
    /// <summary>
    /// Dictionary implementing the hot swap model. Is a completely thread safe collection
    /// with lock-less read and very slow write operations.
    /// </summary>
    /// [DebuggerStepThrough]
    public class HotSwapDictionary<TKey, TValue> : IDictionary<TKey, TValue>
    {
        volatile Dictionary<TKey, TValue> _instance = new Dictionary<TKey, TValue>();

        /// <summary>
        /// Constructor.
        /// </summary>
        public HotSwapDictionary()
        {
        }

        #region Additional specific methods

        /// <summary>
        /// Slow, performs a *hot swap*.
        /// </summary>
        public bool AddRange(TKey[] keys, TValue[] values)
        {
            if (keys.Length != values.Length)
            {
                return false;
            }

            lock (this)
            {
                Dictionary<TKey, TValue> instance = new Dictionary<TKey, TValue>(_instance);
                for (int i = 0; i < keys.Length; i++)
                {
                    if (instance.ContainsKey(keys[i]) == false)
                    {
                        instance.Add(keys[i], values[i]);
                    }
                    else
                    {
                        instance[keys[i]] = values[i];
                    }
                }

                _instance = instance;
            }

            return true;
        }

        /// <summary>
        /// Custom operation, performs a result able add.
        /// Will return false if the key is already added.
        /// </summary>
        /// <returns></returns>
        public bool TryAddValue(TKey key, TValue value)
        {
            lock (this)
            {
                Dictionary<TKey, TValue> instance = new Dictionary<TKey, TValue>(_instance);
                if (instance.ContainsKey(key))
                {
                    return false;
                }

                instance.Add(key, value);
                _instance = instance;
            }

            return true;
        }

        /// <summary>
        /// Will try to get the value with this key, or if it does not exist, add the newly provided value.
        /// </summary>
        public TValue GetOrAdd(TKey key, TValue newValue)
        {
            TValue result;
            lock (this)
            {
                if (TryGetValue(key, out result) == false)
                {
                    Add(key, newValue);
                    result = newValue;
                }
            }

            return result;
        }

        ///// <summary>
        ///// Use this in cases where you need to check for value and retrieve it.
        ///// </summary>
        //public bool TryGetValue(TKey key, ref TValue value)
        //{
        //    Dictionary<TKey, TValue> instance = _instance;

        //    if (instance.ContainsKey(key))
        //    {
        //        value = instance[key];
        //        return true;
        //    }

        //    return false;
        //}

        #endregion

        #region IDictionary<TKEy,TValue> Members

        public ICollection<TKey> Keys
        {
            get { return _instance.Keys; }
        }

        public ICollection<TValue> Values
        {
            get { return _instance.Values; }
        }

        /// <summary>
        /// Slow, performs a *hot swap*.
        /// </summary>
        public void Add(TKey key, TValue value)
        {
            TryAddValue(key, value);
        }

        /// <summary>
        /// Slow, performs a *hot swap*.
        /// </summary>
        public bool Remove(TKey key)
        {
            lock (this)
            {
                Dictionary<TKey, TValue> instance = new Dictionary<TKey, TValue>(_instance);
                if (instance.Remove(key))
                {
                    _instance = instance;
                    return true;
                }
            }

            return false;
        }

        public bool ContainsKey(TKey key)
        {
            return _instance.ContainsKey(key);
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            return _instance.TryGetValue(key, out value);
        }

        /// <summary>
        /// Set is *slow*, performs a *hot swap*.
        /// </summary>
        public TValue this[TKey key]
        {
            get
            {
                return _instance[key];
            }

            set
            {
                lock (this)
                {
                    Dictionary<TKey, TValue> instance = new Dictionary<TKey, TValue>(_instance);
                    instance[key] = value;
                    _instance = instance;
                }
            }
        }

        #endregion

        #region ICollection<KeyValuePair<TKEy,TValue>> Members

        public int Count
        {
            get { return _instance.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        /// <summary>
        /// Slow, performs a *hot swap*.
        /// </summary>
        public void Add(KeyValuePair<TKey, TValue> item)
        {
            this.Add(item.Key, item.Value);
        }

        /// <summary>
        /// Slow, performs a *hot swap*.
        /// </summary>
        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            return this.Remove(item.Key);
        }

        /// <summary>
        /// Slow, performs a *hot swap*.
        /// </summary>
        public void Clear()
        {
            lock (this)
            {
                _instance = new Dictionary<TKey, TValue>();
            }
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return _instance.ContainsKey(item.Key);
        }

        /// <summary>
        /// *Not implemented.
        /// </summary>
        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region IEnumerable<KeyValuePair<TKEy,TValue>> Members

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return _instance.GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _instance.GetEnumerator();
        }

        #endregion
    }
}
