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
using Wintellect.PowerCollections;

namespace Matrix.Framework.MessageBus.Core
{
    /// <summary>
    /// The stamp contains transporting information regarding the path of the envelope.
    /// </summary>
    [Serializable]
    public class EnvelopeTransportation : ISerializable, ICloneable
    {
        Deque<EnvelopeStamp> _stamps;

        /// <summary>
        /// Count of stamps inside this transport info.
        /// </summary>
        public int StampsCount
        {
            get { return _stamps.Count; }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public EnvelopeTransportation()
        {
            _stamps = new Deque<EnvelopeStamp>();
        }

        #region ISerializable Members

        /// <summary>
        /// Implementing the ISerializable to provide a faster, more optimized
        /// serialization for the class.
        /// </summary>
        public EnvelopeTransportation(SerializationInfo info, StreamingContext context)
        {
            // Get from the info.
            SerializationReader reader = new SerializationReader((byte[])info.GetValue("data", typeof(byte[])));

            object[] stamps = reader.ReadObjectArray();
            if (stamps.Length == 0)
            {
                _stamps = new Deque<EnvelopeStamp>();
            }
            else
            {
                _stamps = new Deque<EnvelopeStamp>((EnvelopeStamp[])stamps);
            }
        }

        /// <summary>
        /// Implementing the ISerializable to provide a faster, more optimized
        /// serialization for the class.
        /// </summary>
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            SerializationWriter writer = new SerializationWriter();

            lock (_stamps)
            {
                writer.Write((object[])_stamps.ToArray());
            }

            // Put to the info.
            info.AddValue("data", writer.ToArray());
        }

        #endregion

        /// <summary>
        /// 
        /// </summary>
        public void PushStamp(EnvelopeStamp stamp)
        {
            lock (_stamps)
            {
                _stamps.Add(stamp);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public EnvelopeStamp PopStamp()
        {
            if (_stamps.Count > 0)
            {
                lock (_stamps)
                {
                    if (_stamps.Count > 0)
                    {
                        return _stamps.RemoveFromFront();
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Create a responding envelope transport.
        /// </summary>
        public static EnvelopeTransportation CreateResponseTransport(EnvelopeTransportation transport)
        {
            EnvelopeTransportation result = new EnvelopeTransportation();
            List<EnvelopeStamp> responseStamps = new List<EnvelopeStamp>();
            
            lock (transport._stamps)
            {
                for (int i = transport._stamps.Count - 1; i >= 0; i--)
                {
                    EnvelopeStamp sourceStamp = transport._stamps[i];
                    EnvelopeStamp newStamp = new EnvelopeStamp(sourceStamp.MessageBusStampId, sourceStamp.SenderIndex, sourceStamp.ReceiverIndex);
                    result._stamps.Add(newStamp);
                }
            }

            return result;
        }

        /// <summary>
        /// Clone the envelope transport.
        /// </summary>
        public EnvelopeTransportation Duplicate()
        {
            lock (_stamps)
            {
                return new EnvelopeTransportation() { _stamps = new Deque<EnvelopeStamp>(this._stamps) };
            }
        }

        #region ICloneable Members

        object ICloneable.Clone()
        {
            return Duplicate();
        }

        #endregion
    }
}
