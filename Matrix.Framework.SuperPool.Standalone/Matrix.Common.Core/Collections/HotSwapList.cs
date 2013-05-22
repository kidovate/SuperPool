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
using System.Collections.ObjectModel;

namespace Matrix.Common.Core.Collections
{
    /// <summary>
    /// Hot swapping.
    /// To evade locking - when adding new items, simply replace the list with a new one.
    /// </summary>
    [Serializable]
    public class HotSwapList<TType> : IList<TType>
    {
        // The internal instance of the list. This is where items are actually kept.
        // This instance will never be changed, only swapped when a new one is generated.
        volatile List<TType> _instance = new List<TType>();

        public List<TType> CurrentInstance
        {
            get
            {
                return _instance;
            }
        }

        public delegate void CollectionUpdateDelegate(HotSwapList<TType> list);
        public delegate void ItemUpdateDelegate(HotSwapList<TType> list, TType item);
        public delegate void ItemsUpdateDelegate(HotSwapList<TType> list, IEnumerable<TType> items);

        [field: NonSerialized]
        public event CollectionUpdateDelegate CollectionUpdatedEvent;

        [field: NonSerialized]
        public event ItemUpdateDelegate ItemAddedEvent;

        [field: NonSerialized]
        public event ItemsUpdateDelegate ItemsAddedEvent;

        [field: NonSerialized]
        public event ItemUpdateDelegate ItemRemovedEvent;

        [field: NonSerialized]
        public event ItemsUpdateDelegate ItemsRemovedEvent;

        /// <summary>
        /// Constructor.
        /// </summary>
        public HotSwapList()
        {
        }

        protected void RaiseCollectionUpdateEvent()
        {
            CollectionUpdateDelegate del = CollectionUpdatedEvent;
            if (del != null)
            {
                del(this);
            }
        }

        /// <summary>
        /// This allows to quickly capture a read only version of the 
        /// collection that can be further used as needed in any scenario.
        /// </summary>
        /// <returns></returns>
        public ReadOnlyCollection<TType> AsReadOnly()
        {
            return _instance.AsReadOnly();
        }

        /// <summary>
        /// Add item only if it does not already exist.
        /// </summary>
        /// <param name="item"></param>
        /// <returns>True if the add was performed, or false if it already exists.</returns>
        public bool AddUnique(TType item)
        {
            lock (this)
            {
                if (_instance.Contains(item))
                {
                    return false;
                }

                List<TType> items = new List<TType>(_instance);
                items.Add(item);
                _instance = items;
            }

            ItemUpdateDelegate del = ItemAddedEvent;
            if (del != null)
            {
                del(this, item);
            }

            RaiseCollectionUpdateEvent();

            return true;
        }

        /// <summary>
        /// Try to obtain a value with this index, return false if we fail and no modification to value done.
        /// </summary>
        /// <param name="index">The index of the item retrieved.</param>
        /// <param name="value">The resulting retrieve value.</param>
        /// <returns>True if the value was retrieved, otherwise false.</returns>
        public bool TryGetValue(int index, ref TType value)
        {
            List<TType> instance = _instance;
            if (instance.Count > index)
            {
                value = instance[index];
                return true;
            }

            return false;
        }

        /// <summary>
        /// Sort the items in the collection (must implement IComparable).
        /// </summary>
        public bool Sort()
        {
            if (ReflectionHelper.IsTypeImplementingInterface(typeof(TType), typeof(IComparable)) == false)
            {
                return false;
            }

            lock (this)
            {
                SortedList<TType, TType> items = new SortedList<TType, TType>();
                foreach (TType item in this._instance)
                {
                    items.Add(item, item);
                }

                this.SetToRange(CommonHelper.EnumerableToArray(items.Values));
            }

            RaiseCollectionUpdateEvent();

            return true;
        }

        /// <summary>
        /// Clear all items and add the current badge.
        /// </summary>
        public void SetToRange(IEnumerable<TType> items)
        {
            lock (this)
            {
                List<TType> instance = new List<TType>();
                instance.AddRange(items);
                _instance = instance;
            }

            ItemsUpdateDelegate del = ItemsAddedEvent;
            if (del != null)
            {
                del(this, items);
            }

            RaiseCollectionUpdateEvent();
        }

        public void AddRange(IEnumerable<TType> items)
        {
            lock (this)
            {
                List<TType> instance = new List<TType>(_instance);
                instance.AddRange(items);
                _instance = instance;
            }

            ItemsUpdateDelegate del = ItemsAddedEvent;
            if (del != null)
            {
                del(this, items);
            }

            RaiseCollectionUpdateEvent();
        }
        
        /// <summary>
        /// Remove all instances that are equal, or the same as, this item.
        /// </summary>
        /// <returns>Count of items removed.</returns>
        public int RemoveAll(TType item)
        {
            int result = 0;
            lock (this)
            {
                List<TType> instance = new List<TType>(_instance);
                
                while (instance.Remove(item))
                {
                    result++;
                }

                if (result != 0)
                {
                    _instance = instance;
                }
            }

            ItemUpdateDelegate del = ItemRemovedEvent;
            if (result > 0 && del != null)
            {
                del(this, item);
            }

            RaiseCollectionUpdateEvent();

            return result;
        }


        #region IList<TType> Members

        public int IndexOf(TType item)
        {
            return _instance.IndexOf(item);
        }

        public void Insert(int index, TType item)
        {
            lock (this)
            {
                List<TType> items = new List<TType>(_instance);
                items.Insert(index, item);
                _instance = items;
            }

            ItemUpdateDelegate del = ItemAddedEvent;
            if (del != null)
            {
                del(this, item);
            }

            RaiseCollectionUpdateEvent();
        }

        /// <summary>
        /// Implementation has internal check security,
        /// so no exceptions occur.
        /// </summary>
        /// <param name="index"></param>
        public void RemoveAt(int index)
        {
            TType item;
            lock (this)
            {
                if (_instance.Count > index)
                {
                    List<TType> items = new List<TType>(_instance);
                    item = items[index];
                    items.RemoveAt(index);
                    _instance = items;
                }
                else
                {
                    return;
                }

            }

            ItemUpdateDelegate del = ItemRemovedEvent;
            if (del != null)
            {
                del(this, item);
            }

            RaiseCollectionUpdateEvent();
        }

        /// <summary>
        /// *Warning* setting a value if very slow, since it redoes the hotswaps
        /// the entire collection too, so use with caution.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public TType this[int index]
        {
            get
            {
                return _instance[index];
            }

            set
            {
                lock (this)
                {
                    List<TType> items = new List<TType>(_instance);
                    items[index] = value;
                    _instance = items;
                }
            }
        }

        #endregion

        #region ICollection<TType> Members

        /// <summary>
        /// This method demonstrates the operation of the hot-swap mechanism.
        /// The existing instance is kept, for as long as the new one is 
        /// created, then a fast swap is made.
        /// </summary>
        public void Add(TType item)
        {
            lock (this)
            {
                List<TType> items = new List<TType>(_instance);
                items.Add(item);
                _instance = items;
            }

            ItemUpdateDelegate del = ItemAddedEvent;
            if (del != null)
            {
                del(this, item);
            }

            RaiseCollectionUpdateEvent();
        }

        public bool Remove(TType item)
        {
            lock (this)
            {
                if (_instance.Contains(item) == false)
                {
                    return false;
                }

                List<TType> items = new List<TType>(_instance);
                items.Remove(item);
                _instance = items;
            }

            ItemUpdateDelegate del = ItemRemovedEvent;
            if (del != null)
            {
                del(this, item);
            }

            RaiseCollectionUpdateEvent();

            return true;
        }

        public void Clear()
        {
            IEnumerable<TType> items;
            lock (this)
            {
                items = _instance;
                _instance = new List<TType>();
            }

            ItemsUpdateDelegate del = ItemsRemovedEvent;
            if (del != null)
            {
                del(this, items);
            }

            RaiseCollectionUpdateEvent();
        }

        public bool Contains(TType item)
        {
            return _instance.Contains(item);
        }

        public void CopyTo(TType[] array, int arrayIndex)
        {
            _instance.CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// This is a typical example of the operation of the hot swap list.
        /// Internal instance is used for data access.
        /// </summary>
        public int Count
        {
            get { return _instance.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }


        #endregion

        #region IEnumerable<TType> Members

        public IEnumerator<TType> GetEnumerator()
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
