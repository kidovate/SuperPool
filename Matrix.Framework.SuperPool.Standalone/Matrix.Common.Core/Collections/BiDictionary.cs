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
    /// Implements a simple bi-directional dictionary (or a bi-map). Allows to search by key or by value.
    /// Implementation is done by 2 internal dictionaries that are synchronized to work together.
    /// </summary>
    /// <typeparam name="ValueType1"></typeparam>
    /// <typeparam name="ValueType2"></typeparam>
    [Serializable]
    public class BiDictionary<ValueTypeKey, ValueTypeValue> : IEnumerable<KeyValuePair<ValueTypeKey, ValueTypeValue>>
    {
        Dictionary<ValueTypeKey, ValueTypeValue> _dictionary1 = new Dictionary<ValueTypeKey, ValueTypeValue>();
        Dictionary<ValueTypeValue, ValueTypeKey> _dictionary2 = new Dictionary<ValueTypeValue, ValueTypeKey>();

        /// <summary>
        /// [] accessor.
        /// </summary>
        public ValueTypeValue this[ValueTypeKey value]
        {
            get { return _dictionary1[value]; }
        }

        /// <summary>
        /// Count of elements in collection.
        /// </summary>
        public int Count
        {
            get { return _dictionary1.Count; }
        }

        public IEnumerable<KeyValuePair<ValueTypeKey, ValueTypeValue>> Pairs
        {
            get
            {
                return _dictionary1;
            }
        }

        public IEnumerable<ValueTypeKey> Keys
        {
            get { return _dictionary1.Keys; }
        }

        public IEnumerable<ValueTypeValue> Values
        {
            get { return _dictionary1.Values; }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public BiDictionary()
        {
        }

        /// <summary>
        /// Add element.
        /// </summary>
        public bool Add(ValueTypeKey keyValue, ValueTypeValue valueValue)
        {
            if (_dictionary1.ContainsKey(keyValue) || _dictionary2.ContainsKey(valueValue))
            {
                return false;
            }

            _dictionary1.Add(keyValue, valueValue);
            _dictionary2.Add(valueValue, keyValue);

            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        public ValueTypeValue GetByKey(ValueTypeKey key)
        {
            return _dictionary1[key];
        }

        /// <summary>
        /// 
        /// </summary>
        public object GetByKeyNullabe(ValueTypeKey key)
        {
            if (_dictionary1.ContainsKey(key) == false)
            {
                return null;
            }

            return _dictionary1[key];
        }


        /// <summary>
        /// Safe.
        /// </summary>
        public bool TryGetByKey(ValueTypeKey key, ref ValueTypeValue value)
        {
            if (_dictionary1.ContainsKey(key) == false)
            {
                return false;
            }

            value = _dictionary1[key];
            return true;
        }


        /// <summary>
        /// 
        /// </summary>
        public ValueTypeKey GetByValue(ValueTypeValue value)
        {
            return _dictionary2[value];
        }

        /// <summary>
        /// 
        /// </summary>
        public bool ContainsValue(ValueTypeValue value)
        {
            return _dictionary1.ContainsValue(value);
        }

        /// <summary>
        /// 
        /// </summary>
        public bool ContainsKey(ValueTypeKey key)
        {
            return _dictionary1.ContainsKey(key);
        }

        /// <summary>
        /// 
        /// </summary>
        public object GetByValueNullabe(ValueTypeValue value)
        {
            if (_dictionary2.ContainsKey(value) == false)
            {
                return null;
            }

            return _dictionary2[value];
        }


        /// <summary>
        /// Safe.
        /// </summary>
        public bool TryGetByValue(ValueTypeValue key, ref ValueTypeKey value)
        {
            if (_dictionary2.ContainsKey(key) == false)
            {
                return false;
            }

            value = _dictionary2[key];
            return true;
        }

        public void Clear()
        {
            _dictionary1.Clear();
            _dictionary2.Clear();
        }

        public bool RemoveByKey(ValueTypeKey value)
        {
            if (_dictionary1.ContainsKey(value))
            {
                _dictionary2.Remove(_dictionary1[value]);
                return _dictionary1.Remove(value);
            }

            return false;
        }

        public bool RemoveByValue(ValueTypeValue value)
        {
            if (_dictionary2.ContainsKey(value))
            {
                _dictionary1.Remove(_dictionary2[value]);
                return _dictionary2.Remove(value);
            }

            return false;
        }


        #region IEnumerable<KeyValuePair<ValueTypeKey,ValueTypeValue>> Members

        public IEnumerator<KeyValuePair<ValueTypeKey, ValueTypeValue>> GetEnumerator()
        {
            return _dictionary1.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _dictionary1.GetEnumerator();
        }

        #endregion
    }
}
