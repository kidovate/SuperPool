// -----
// Copyright 2010 Deyan Timnev
// This file is part of the Matrix Platform (www.matrixplatform.com).
// The Matrix Platform is free software: you can redistribute it and/or modify it under the terms of the GNU Lesser General Public License as published by the Free Software Foundation, 
// either version 3 of the License, or (at your option) any later version. The Matrix Platform is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; 
// without even the implied warranty of  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Lesser General Public License for more details.
// You should have received a copy of the GNU Lesser General Public License along with the Matrix Platform. If not, see http://www.gnu.org/licenses/lgpl.html
// -----
using System;

namespace Matrix.Common.Core.Identification
{
    /// <summary>
    /// Serves as a basis for multiple types of identificantion classes.
    /// The concept is to provide a unified way of undentifying elements/instances etc.
    /// This is the top of the component hierarchy.
    /// </summary>
    [Serializable]
    //[TypeConverterAttribute(typeof(ExpandableObjectConverter))]
    public class ComponentId : IComparable<ComponentId>, IEquatable<ComponentId>
    {
        /// <summary>
        /// Unique GUID of the client.
        /// </summary>
        public Guid Guid { get; protected set; }

        /// <summary>
        /// Name of the client.
        /// </summary>
        public string Name { get; protected set; }
        
        /// <summary>
        /// Is this an empty instance (not actual value).
        /// </summary>
        public bool IsEmpty
        {
            get { return this.Guid == Guid.Empty; }
        }

        /// <summary>
        /// Empty value.
        /// </summary>
        public static ComponentId Empty
        {
            get { return new ComponentId(Guid.Empty, string.Empty); }
        }

        /// <summary>
        /// Default parameterless constructor (may be used in XML serialization).
        /// </summary>
        public ComponentId():
            this(Guid.Empty, string.Empty)
        {
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public ComponentId(Guid guid, string name)
        {
            this.Guid = guid;
            this.Name = name;
        }

        /// <summary>
        /// Comparition operator.
        /// </summary>
        public static bool operator ==(ComponentId a, ComponentId b)
        {
            if (((object)a != null) && ((object)b != null))
            {
                return a.Equals(b);
            }

            if ((object)a == null && (object)b == null)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        public static bool operator !=(ComponentId a, ComponentId b)
        {
            return !(a == b);
        }
        
        /// <summary>
        /// Override the default equals, to handle operations within collections.
        /// </summary>
        public override bool Equals(object obj)
        {
            return Equals((ComponentId)obj);
        }

        /// <summary>
        /// Override the default equals, to handle operations within collections.
        /// </summary>
        public override int GetHashCode()
        {
            return Guid.GetHashCode();
        }

        /// <summary>
        /// Print general information related to this object.
        /// </summary>
        /// <returns></returns>
        public virtual string Print()
        {
            return Name + "[" + Guid.ToString() + "]";
        }

        public override string ToString()
        {
            return Print();
        }

        #region IComparable<Id> Members

        /// <summary>
        /// Compare is done partial (Guid only).
        /// </summary>
        public int CompareTo(ComponentId other)
        {
            return Guid.CompareTo(other.Guid);
        }

        #endregion

        #region IEquatable<Id> Members

        /// <summary>
        /// Equal is done partial (Guid only).
        /// </summary>
        public bool Equals(ComponentId other)
        {
            if (other == null)
            {
                return false;
            }

            return (Guid.Equals(other.Guid));
        }

        #endregion
    }
}
