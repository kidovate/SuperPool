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

namespace Matrix.Common.Core.Serialization
{
    /// <summary>
    /// Base interface for all serializers. Used when an object needs to be converted to byte[] or stream
    /// so that it can be transported, by default is a binary serialization model.
    /// </summary>
    public interface ISerializer
    {
        /// <summary>
        /// Generate a copy of this item.
        /// By default will reuse the Serialize() and Deserialize methods.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        object Duplicate(object item);
        
        /// <summary>
        /// Serialize object to stream.
        /// Make sure the stream is used only for this operation, 
        /// since it may get closed during the operation.
        /// *Throws*
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="p"></param>
        /// <returns></returns>
        bool Serialize(MemoryStream stream, object p);

        /// <summary>
        /// Deserialize object from stream.
        /// *Throws* InvalidDataException() on invalid data.
        /// Make sure the stream is used only for this operation, 
        /// since it may get closed during the operation.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        object Deserialize(MemoryStream stream);
    }
}
