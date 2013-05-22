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
using Matrix.Framework.SuperPool.Core;

namespace Matrix.Framework.SuperPool.Demonstration
{
    /// <summary>
    /// Helper delegate with one string parameter.
    /// </summary>
    /// <param name="parameter1"></param>
    public delegate object HelperDelegate(string parameter1);

    /// <summary>
    /// This interface defines how the communication is done.
    /// We have defined one event and one method, to test them both.
    /// 
    /// This can be replaced with any interface of your choice... 
    /// to define how the communication between any 2 super pool
    /// components will look like. A single component can have
    /// as many interfaces as needed.
    /// </summary>
    [SuperPoolInterface]
    public interface ICommunicationInterface
    {
        event HelperDelegate EventOne;
        string DoWork(string parameter1);
    }
}
