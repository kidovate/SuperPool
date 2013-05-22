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
using Matrix.Framework.MessageBus.Net.Messages;

namespace Matrix.Framework.MessageBus.Net
{
    /// <summary>
    /// Client side access control structure, stores information on client login.
    /// </summary>
    [Serializable]
    public sealed class ClientAccessControl
    {
        public string Username { get; set; }
        public string Password { get; set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        public ClientAccessControl()
        {
        }

        /// <summary>
        /// Obtain message containing the info of this control instance.
        /// This is used from the client side to send
        /// </summary>
        /// <returns></returns>
        public AccessMessage ObtainClientSideMessage()
        {
            return new AccessMessage() { Username = this.Username, Password = this.Password };
        }

        /// <summary>
        /// Update control with incoming message data.
        /// </summary>
        public void Update(AccessMessage message)
        {
            Username = message.Username;
            Password = message.Password;
        }
    }
}
