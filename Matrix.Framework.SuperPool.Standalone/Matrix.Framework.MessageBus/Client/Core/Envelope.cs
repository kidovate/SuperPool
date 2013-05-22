// -----
// Copyright 2010 Deyan Timnev
// This file is part of the Matrix Platform (www.matrixplatform.com).
// The Matrix Platform is free software: you can redistribute it and/or modify it under the terms of the GNU Lesser General Public License as published by the Free Software Foundation, 
// either version 3 of the License, or (at your option) any later version. The Matrix Platform is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; 
// without even the implied warranty of  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Lesser General Public License for more details.
// You should have received a copy of the GNU Lesser General Public License along with the Matrix Platform. If not, see http://www.gnu.org/licenses/lgpl.html
// -----
using System;
using System.Runtime.Serialization;
using Matrix.Common.Core.Serialization;
using Matrix.Common.Extended.FastSerialization;

#if Matrix_Diagnostics
using Matrix.Common.Diagnostics;
#endif


namespace Matrix.Framework.MessageBus.Core
{
    /// <summary>
    /// Envelope stores a message.
    /// </summary>
    [Serializable]
    public class Envelope
    {
        /// <summary>
        /// The type of duplication (or cloning), required to be done.
        /// </summary>
        public enum DuplicationModeEnum : int
        {
            /// <summary>
            /// Duplicate nothing.
            /// </summary>
            None,
            /// <summary>
            /// Duplicate the message instance only.
            /// </summary>
            DuplicateMessage,
            /// <summary>
            /// Duplicate the envelope instance.
            /// </summary>
            DuplicateEnvelope,
            /// <summary>
            /// Duplicate the envelope instance and the message instance.
            /// </summary>
            DuplicateBoth
        }

        /// <summary>
        /// Determine the type of execution to perform on the client.
        /// </summary>
        public enum ExecutionModelEnum
        {
            Default, // Execute the item by the default execution engine on the receiver.
            Direct // Execute the item directly on the receiver, bypassing the execution engine.
        }

        DuplicationModeEnum _duplicationMode = DuplicationModeEnum.None;
        /// <summary>
        /// Determines how (if any) duplication will be performed on the envelope and its data.
        /// By default value is None, meaning everything is transported using references.
        /// Duplication is mandatory (DuplicateBoth) when transporting to a remote location trough TCP.IP.
        /// </summary>
        public DuplicationModeEnum DuplicationMode
        {
            get { return _duplicationMode; }
            set { _duplicationMode = value; }
        }

        volatile object _message = null;
        /// <summary>
        /// The actual message.
        /// </summary>
        public object Message
        {
            get { return _message; }
            set { _message = value; }
        }

        ExecutionModelEnum _executionModel = ExecutionModelEnum.Default;
        /// <summary>
        /// Instructions on how to execute the item.
        /// </summary>
        public ExecutionModelEnum ExecutionModel
        {
            get { return _executionModel; }
            set { _executionModel = value; }
        }

        volatile EnvelopeTransportation _transportHistory = new EnvelopeTransportation();
        /// <summary>
        /// A history of the locations the item has visited.
        /// </summary>
        public EnvelopeTransportation History
        {
            get { return _transportHistory; }
        }

        volatile EnvelopeTransportation _transportTargetAddress = null;
        /// <summary>
        /// Information regarding transporting the item to somewhere.
        /// </summary>
        public EnvelopeTransportation Address
        {
            get { return _transportTargetAddress; }
            set { _transportTargetAddress = value; }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public Envelope()
        {
        }

        /// <summary>
        /// Detailed constructor.
        /// </summary>
        public Envelope(object message)
        {
            _message = message;
        }

        /// <summary>
        /// Implementing the ISerializable to provide a faster, more optimized
        /// serialization for the class using the fast serialization elements.
        /// </summary>
        public Envelope(SerializationInfo info, StreamingContext context)
        {
            // Get from the info.
            SerializationReader reader = new SerializationReader((byte[])info.GetValue("data", typeof(byte[])));

            _duplicationMode = (DuplicationModeEnum)reader.ReadInt32();
            _executionModel = (ExecutionModelEnum)reader.ReadInt32();
            _message = reader.ReadObject();
            _transportHistory = (EnvelopeTransportation)reader.ReadObject();
            _transportTargetAddress = (EnvelopeTransportation)reader.ReadObject();
        }

        /// <summary>
        /// Implementing the ISerializable to provide a faster, more optimized
        /// serialization for the class.
        /// </summary>
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            SerializationWriter writer = new SerializationWriter();

            writer.Write((int)_duplicationMode);
            writer.Write((int)_executionModel);
            writer.WriteObject(_message);
            writer.WriteObject(_transportHistory);
            writer.WriteObject(_transportTargetAddress);

            // Put to the info.
            info.AddValue("data", writer.ToArray());
        }

        /// <summary>
        /// This may actually *not* duplicate the object, since it follows the DuplicationMode.
        /// </summary>
        /// <returns></returns>
        public virtual Envelope Duplicate(ISerializer serializer)
        {
            Envelope newEnvelope = this;

            if (_duplicationMode == DuplicationModeEnum.DuplicateEnvelope
                || _duplicationMode == DuplicationModeEnum.DuplicateBoth)
            {
                //newObject = (Envelope)this.MemberwiseClone();
                //newObject = (Envelope)serializer.Duplicate(this);

                newEnvelope = new Envelope() { _duplicationMode = this._duplicationMode };

                EnvelopeTransportation transportHistory = _transportHistory;
                EnvelopeTransportation transportTargetAddress = _transportTargetAddress;

                if (transportHistory != null)
                {
                    newEnvelope._transportHistory = transportHistory.Duplicate();
                }

                if (transportTargetAddress != null)
                {
                    newEnvelope._transportTargetAddress = transportTargetAddress.Duplicate();
                }
            }
            
            if (_message != null &&
                (_duplicationMode == DuplicationModeEnum.DuplicateMessage
                 || _duplicationMode == DuplicationModeEnum.DuplicateBoth))
            {
                if (_message is ICloneable)
                {
                    newEnvelope._message = ((ICloneable)_message).Clone();
                }
                else if (_message.GetType().IsClass)
                {// We need to use the slow cloning mechanism.
                    newEnvelope._message = serializer.Duplicate(_message);
#if Matrix_Diagnostics
                    SystemMonitor.OperationErrorIf(newEnvelope._message == null, "Failed to serialize message [" + _message.GetType().Name + "].");
#endif
                }

                // Value type items are supposed to be copied by referencing.
            }

            return newEnvelope;
        }

    }
}
