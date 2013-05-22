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
using System.Reflection;
using Matrix.Framework.SuperPool.Subscription;

namespace Matrix.Framework.SuperPool.Core
{
    /// <summary>
    /// Defines interface to allow separate super pool instances to communicate over a message bus.
    /// Mostly applied to allow to transport the subscription info over.
    /// 
    /// NOTE: event subcriptions can not be done trough this communication, since
    /// it would possibly lead to indefinite cycle.
    /// 
    /// NOTE: needs to be PUBLIC, in order for the super pool proxy builder to access it.
    /// </summary>
    [SuperPoolInterface]
    public interface ISuperPoolIntercom
    {
        void ProcessSubscriptionUpdate(EventSubscriptionRequest subscriptionRequest);
    }
}
