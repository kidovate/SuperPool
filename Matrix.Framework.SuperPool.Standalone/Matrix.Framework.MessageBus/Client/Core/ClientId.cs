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
using Matrix.Common.Core.Identification;
using System.Runtime.Serialization;
using Matrix.Common.Extended.FastSerialization;

namespace Matrix.Framework.MessageBus.Core
{
    /// <summary>
    /// Describes the identity of a message bus client.
    /// </summary>
    [Serializable]
    public class ClientId : ComponentId, ISerializable, ICloneable, IComparable<ClientId>, IEquatable<ClientId>
    {
        public const int InvalidMessageBusClientIndex = -1;

        int _messageBusIndex = InvalidMessageBusClientIndex;
        /// <summary>
        /// The message bus index.
        /// </summary>
        internal int LocalMessageBusIndex
        {
            get { return _messageBusIndex; }
            set { _messageBusIndex = value; }
        }

        [NonSerialized]
        IMessageBus _messageBus = null;

        /// <summary>
        /// Instance of the message bus that this id belongs to, not persisted.
        /// </summary>
        internal IMessageBus MessageBus
        {
            get { return _messageBus; }
            set { _messageBus = value; }
        }

        /// <summary>
        /// Is the message bus index valid (invalid usually for remote clients).
        /// </summary>
        public bool IsMessageBusIndexValid
        {
            get { return _messageBusIndex != InvalidMessageBusClientIndex; }
        }

        /// <summary>
        /// Is this the Id of a local client, or a remote one.
        /// </summary>
        public bool IsLocalClientId
        {
            get
            {
                return _messageBus != null && IsMessageBusIndexValid;
            }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public ClientId(string name)
            : base(Guid.NewGuid(), name)
        {
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public ClientId(Guid guid, string name)
            : base(guid, name)
        {
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public ClientId(ComponentId id)
            : base(id.Guid, id.Name)
        {
        }
        
        /// <summary>
        /// 
        /// </summary>
        public override string ToString()
        {
            return string.Format("Name [{0}], Guid [{1}]", Name, Guid.ToString());
        }

        #region IComparable<MessageBusClientId> Members

        public int CompareTo(ClientId other)
        {
            return Guid.CompareTo(other.Guid);
        }

        #endregion

        #region IEquatable<MessageBusClientId> Members

        public bool Equals(ClientId other)
        {
            return CompareTo(other) == 0;
        }

        #endregion

        /// <summary>
        /// We shall use the Guid code, since this allows us to 
        /// compare and operate on Dictionary/Map containers.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return this.Guid.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is ClientId)
            {
                return Equals((ClientId)obj);
            }

            return base.Equals(obj);
        }

        #region ISerializable Members
        
        public ClientId(SerializationInfo info, StreamingContext context)
        {
            // Get from the info.
            SerializationReader reader = new SerializationReader((byte[])info.GetValue("data", typeof(byte[])));

            Guid = reader.ReadGuid();
            Name = reader.ReadString();
            _messageBusIndex = reader.ReadInt32();
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            SerializationWriter writer = new SerializationWriter();

            writer.Write(Guid);
            writer.Write(Name);
            writer.Write(_messageBusIndex);

            // Put to the info.
            info.AddValue("data", writer.ToArray());
        }

        #endregion

        public ClientId Duplicate()
        {
            return new ClientId(this.Name) { _messageBus = this._messageBus, _messageBusIndex = this._messageBusIndex };
        }

        #region ICloneable Members

        object ICloneable.Clone()
        {
            return Duplicate();
        }

        #endregion

    }
}
