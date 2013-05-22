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

namespace Matrix.Framework.MessageBus.Net
{
    /// <summary>
    /// Describes information for the server side control of access, required
    /// when authentication is applied for client message bus is connecting 
    /// to the server message bus.
    /// </summary>
    [Serializable]
    public class ServerAccessControl
    {
        public string Username { get; set; }
        public string Password { get; set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        public ServerAccessControl()
        {
        }

        /// <summary>
        /// Is the supplied control under conditions of current control.
        /// </summary>
        /// <param name="control"></param>
        /// <returns></returns>
        public virtual bool IsAllowed(ClientAccessControl control)
        {
            if (string.IsNullOrEmpty(Username) && string.IsNullOrEmpty(Password))
            {
                return true;
            }

            if (control == null)
            {
                return false;
            }

            if (string.IsNullOrEmpty(Username))
            {
                return Password == control.Password;
            }

            return Username == control.Username && Password == control.Password;
        }

    }
}
