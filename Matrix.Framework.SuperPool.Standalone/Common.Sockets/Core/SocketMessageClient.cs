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
using System.Net.Sockets;
using System.Net;
using Matrix.Common.Core.Serialization;
using Matrix.Common.Sockets.Common;

namespace Matrix.Common.Sockets.Core
{
    /// <summary>
    /// Main message client class, allows to transport 
    /// messages across a TCP.IP connection.
    /// </summary>
    public class SocketMessageClient : SocketClientCommunicator
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="endPoint"></param>
        public SocketMessageClient(ISerializer serializer)
            : base(serializer)
        {
        }

        /// <summary>
        /// Constructor, with auto connect enabled.
        /// </summary>
        public SocketMessageClient(EndPoint endPoint, ISerializer serializer)
            : base(endPoint, serializer)
        {
        }

    }
}
