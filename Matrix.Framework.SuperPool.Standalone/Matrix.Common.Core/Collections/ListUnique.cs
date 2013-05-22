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
    /// Extends the bahaviour of the List class to provide single entry mode, etc.
    /// No duplication of item is allowed in the class by default.
    /// </summary>
    /// <typeparam name="TClass"></typeparam>
    [Serializable]
    public class ListUnique<TClass> : List<TClass>
    {
        bool _singleEntryMode = true;
        /// <summary>
        /// An item is allowed to enter only once.
        /// </summary>
        public bool SingleEntryMode
        {
            get { return _singleEntryMode; }
            set { _singleEntryMode = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public ListUnique()
        {
        }

        /// <summary>
        /// Add/Update item entry.
        /// </summary>
        public void UpdateItem(TClass item, bool isAdded)
        {
            if (isAdded)
            {
                Add(item);
            }
            else
            {
                Remove(item);
            }
        }

        /// <summary>
        /// Add operation override.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public new bool Add(TClass item)
        {
            if (SingleEntryMode && this.Contains(item))
            {
                return false;
            }

            base.Add(item);
            return true;
        }

        public new void AddRange(IEnumerable<TClass> collection)
        {
            if (SingleEntryMode)
            {
                List<TClass> items = new List<TClass>();
                foreach (TClass item in collection)
                {
                    if (this.Contains(item) == false)
                    {
                        items.Add(item);
                    }
                }
                base.AddRange(items);
            }
            else
            {
                base.AddRange(collection);
            }
        }

        public new void Insert(int index, TClass item)
        {
            if (SingleEntryMode && this.Contains(item))
            {
                return;
            }

            base.Insert(index, item);
        }

        public new void InsertRange(int index, IEnumerable<TClass> collection)
        {
            if (SingleEntryMode)
            {
                List<TClass> items = new List<TClass>();
                foreach (TClass item in collection)
                {
                    if (this.Contains(item) == false)
                    {
                        items.Add(item);
                    }
                }
                base.InsertRange(index, items);
            }
            else
            {
                base.InsertRange(index, collection);
            }
            
        }

        public void RemoveRange(IEnumerable<TClass> items)
        {
            foreach (TClass item in items)
            {
                base.Remove(item);
            }
        }
    }
}
