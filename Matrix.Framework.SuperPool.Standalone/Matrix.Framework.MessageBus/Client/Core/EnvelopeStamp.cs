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
using System.Runtime.Serialization;
using Matrix.Common.Extended.FastSerialization;

namespace Matrix.Framework.MessageBus.Core
{
    /// <summary>
    /// A stamp is placed by passing trough one message bus.
    /// It can be used both as history, and as indication where 
    /// to send a respoding envelope.
    /// </summary>
    [Serializable]
    public class EnvelopeStamp : ICloneable
    {
        long _stampId = 0;

        /// <summary>
        /// Number of the transport
        /// </summary>
        public long MessageBusStampId
        {
            get { return _stampId; }
            set { _stampId = value; }
        }

        volatile ClientId _receiverId = null;
        /// <summary>
        /// Index of the receiving entity.
        /// </summary>
        public ClientId ReceiverIndex
        {
            get { return _receiverId; }
            set { _receiverId = value; }
        }

        volatile ClientId _senderId = null;
        /// <summary>
        /// Index of the sender entity.
        /// </summary>
        public ClientId SenderIndex
        {
            get { return _senderId; }
            set { _senderId = value; }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public EnvelopeStamp(long stampId, ClientId receiverId, ClientId senderId)
        {
            _stampId = stampId;
            _receiverId = receiverId;
            _senderId = senderId;
        }

        #region ISerializable Members

        /// <summary>
        /// Implementing the ISerializable to provide a faster, more optimized
        /// serialization for the class.
        /// </summary>
        public EnvelopeStamp(SerializationInfo info, StreamingContext context)
        {
            // Get from the info.
            SerializationReader reader = new SerializationReader((byte[])info.GetValue("data", typeof(byte[])));

            _stampId = reader.ReadInt64();
            _receiverId = (ClientId)reader.ReadObject();
            _senderId = (ClientId)reader.ReadObject();
        }

        /// <summary>
        /// Implementing the ISerializable to provide a faster, more optimized
        /// serialization for the class.
        /// </summary>
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            SerializationWriter writer = new SerializationWriter();

            writer.Write(_stampId);
            writer.WriteObject(_receiverId);
            writer.WriteObject(_senderId);

            // Put to the info.
            info.AddValue("data", writer.ToArray());
        }

        #endregion

        public object Clone()
        {
            return Duplicate();
        }

        public EnvelopeStamp Duplicate()
        {
            ClientId senderId = this._senderId;
            ClientId receiverId = this._receiverId;

            if (senderId != null)
            {
                senderId = senderId.Duplicate();
            }

            if (receiverId != null)
            {
                receiverId = receiverId.Duplicate();
            }

            return new EnvelopeStamp(_stampId, senderId, receiverId);
        }

    }
}
