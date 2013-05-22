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

namespace Matrix.Framework.SuperPool.Clients
{
    /// <summary>
    /// Class represents the parameters returned by an async call.
    /// </summary>
    public class AsyncResultParams
    {
        /// <summary>
        /// State object, provided by the user upon the initial call, 
        /// used to track the actual call where needed.
        /// </summary>
        public object State { get; set; }

        /// <summary>
        /// Result of the call, received from a counterparty.
        /// </summary>
        public object Result { get; set; }

        /// <summary>
        /// Exception, in case one occured during the execution on a counterparty.
        /// </summary>
        public Exception Exception { get; set; }

        /// <summary>
        /// Has the call been a failure.
        /// </summary>
        public bool HasException
        {
            get
            {
                return Exception != null;
            }
        }
    }
}
