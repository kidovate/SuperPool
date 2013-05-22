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

namespace Matrix.Framework.MessageBus.Net.Messages
{
    /// <summary>
    /// Base class for message bus messages.
    /// </summary>
    [Serializable]
    public class Message
    {
        public int MessageId { get; set; }
        public bool RequestResponse { get; set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        public Message()
        {
            RequestResponse = false;
        }
    }
}
