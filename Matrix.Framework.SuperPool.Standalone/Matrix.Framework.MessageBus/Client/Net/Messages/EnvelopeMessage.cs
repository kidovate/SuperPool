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
using Matrix.Framework.MessageBus.Core;

namespace Matrix.Framework.MessageBus.Net.Messages
{
    /// <summary>
    /// Message stores an envelope, and transports it over.
    /// </summary>
    [Serializable]
    internal class EnvelopeMessage : Message
    {
        public Envelope Envelope { get; set; }
        public ClientId Sender { get; set; }
        public ClientId[] Receivers { get; set; }
    }
}
