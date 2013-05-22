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

namespace Matrix.Framework.SuperPool.Core
{
    /// <summary>
    /// Actual instance-able class of message based super pool.
    /// </summary>
    public class SuperPool : SuperPoolSubscription
    {
        /// <summary>
        /// Constructor. Will create a no named default instance of a message bus.
        /// </summary>
        public SuperPool()
            : this("NoName.SuperPool")
        {
        }

        /// <summary>
        /// Constructor with explicit init. Will create a default instance of the 
        /// common (non network) message bus).
        /// </summary>
        public SuperPool(string name)
        {
            base.Initialize(new Matrix.Framework.MessageBus.Core.MessageBus(name));
        }

        /// <summary>
        /// Constructor with existing message bus.
        /// </summary>
        public SuperPool(IMessageBus messageBus)
        {
            base.Initialize(messageBus);
        }

        public override void Dispose()
        {
            base.Dispose();
        }
    }
}
