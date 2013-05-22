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

namespace Matrix.Common.Extended.Operationals
{
    /// <summary>
    /// Enum with states an object can be in. Only one state at a given moment is typically allowed.
    /// Not every object goes trough all the stages, it is up to itself to determine what state and when to be in.
    /// </summary>
    public enum OperationalStateEnum
    {
        Unknown, // [Reccommendation] State of the item now known.
        Constructed, // [Reccommendation] Object was constructed.
        Initializing, // [Reccommendation] Object is initializing (maybe waiting for additional data).
        Initialized, // [Reccommendation] Object is initialized.
        Operational, // [Reccommendation] Object is ready for operation.
        NotOperational, // [Reccommendation] Object is not ready for operation.
        UnInitialized, // [Reccommendation] Object was uninitialized.
        Disposed // [Reccommendation] Object was disposed.
    }

    /// <summary>
    /// IOperational related delegate.
    /// </summary>
    public delegate void OperationalStateChangedDelegate(IOperational operational, OperationalStateEnum previousOperationState);

    /// <summary>
    /// Interface defines an object that has operational and non operation states.
    /// </summary>
    public interface IOperational
    {
        /// <summary>
        /// The current state of the object.
        /// </summary>
        OperationalStateEnum OperationalState { get; }

        /// <summary>
        /// Raised when operator changes state, the second parameter is the *previous operational* state.
        /// </summary>
        event OperationalStateChangedDelegate OperationalStateChangedEvent;
    }
}
