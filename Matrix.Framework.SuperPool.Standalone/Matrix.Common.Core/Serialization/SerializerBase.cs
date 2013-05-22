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
using System.IO;
using Matrix.Common.Core;

namespace Matrix.Common.Core.Serialization
{
    /// <summary>
    /// Base class for message serializers.
    /// </summary>
    public abstract class SerializerBase : ISerializer
    {
        /// <summary>
        /// 16 MB at the moment (256 ^ 3)
        /// </summary>
        public const int MaxMessageSize = 16777216;

        /// <summary>
        /// Constructor.
        /// </summary>
        protected SerializerBase()
        {
        }

        #region Abstract Members

        protected abstract bool SerializeData(Stream stream, object message);
        protected abstract object DeserializeData(Stream stream);

        #endregion

        public virtual object Duplicate(object item)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                if (SerializeData(stream, item) == false)
                {
                    throw new InvalidDataException("Failed to serialize item [{" + item.ToString() + "}].");
                }

                using(MemoryStream stream2 = new MemoryStream(stream.GetBuffer()))
                {
                    return DeserializeData(stream2);
                }
            }
        }

        /// <summary>
        /// Returns false if failed to serialize.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public bool Serialize(MemoryStream stream, object message)
        {
            if (message == null)
            {
                return false;
            }

            long startPosition = stream.Position;

            // Placeholder.
            byte[] sizeBytes = BitConverter.GetBytes(int.MaxValue);
            stream.Write(sizeBytes, 0, sizeBytes.Length);

            if (SerializeData(stream, message) == false)
            {
                stream.Seek(startPosition, SeekOrigin.Begin);
                return false;
            }

            int length = (int)stream.Position - (int)startPosition;
            
            long lastPosition = stream.Position;
            stream.Seek(startPosition, SeekOrigin.Begin);

            if (length > MaxMessageSize)
            {
                CoreSystemMonitor.OperationError("Message size too big.");
                return false;
            }

            // Encoding (int.MaxValue - value); this helps diagnostics of message transport errors.
            sizeBytes = BitConverter.GetBytes(int.MaxValue - length);
            stream.Write(sizeBytes, 0, sizeBytes.Length);

            stream.Seek(lastPosition, SeekOrigin.Begin);

            return true;
        }

        #region ISerializer Members

        public object Deserialize(MemoryStream stream)
        {
            long startPosition = stream.Position;

            byte[] sizeBytes = new byte[sizeof(int)];
            if (stream.Read(sizeBytes, 0, sizeBytes.Length) != sizeBytes.Length)
            {// Not enough info to extract size.
                stream.Seek(startPosition, SeekOrigin.Begin);
                return null;
            }

            int size = int.MaxValue - BitConverter.ToInt32(sizeBytes, 0);

            if (size > MaxMessageSize)
            {// Invalid size; we do not seek to start since data stream is already corrupt; this will indicate the owner
                // to clear it and try to recover.
                throw new InvalidDataException();
            }

            if (stream.Length - stream.Position + sizeof(int) < size)
            {// Not enough info to extract item.
                stream.Seek(startPosition, SeekOrigin.Begin);
                return null;
            }

            return DeserializeData(stream);
        }

        #endregion

    }
}
