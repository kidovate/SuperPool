// -----
// Copyright 2010 Deyan Timnev
// This file is part of the Matrix Platform (www.matrixplatform.com).
// The Matrix Platform is free software: you can redistribute it and/or modify it under the terms of the GNU Lesser General Public License as published by the Free Software Foundation, 
// either version 3 of the License, or (at your option) any later version. The Matrix Platform is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; 
// without even the implied warranty of  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Lesser General Public License for more details.
// You should have received a copy of the GNU Lesser General Public License along with the Matrix Platform. If not, see http://www.gnu.org/licenses/lgpl.html
// -----
using System;
using System.Runtime.Serialization;

#if Matrix_Diagnostics
using Matrix.Common.Diagnostics;
#endif

namespace Matrix.Common.Extended.Operationals
{
    /// <summary>
    /// Gives some common predefined code for the implementation of the IOperational interface.
    /// </summary>
    [CLSCompliant(false)]
    [Serializable]
    public class Operational : IOperational, IDeserializationCallback
    {
        protected volatile OperationalStateEnum _operationalState = OperationalStateEnum.UnInitialized;
        
        /// <summary>
        /// The current operational state of the object.
        /// </summary>
        public OperationalStateEnum OperationalState
        {
            get { return _operationalState; }
        }

        volatile bool _statusSynchronizationEnabled = true;
        /// <summary>
        /// Is the status synchronization enabled.
        /// </summary>
        protected bool StatusSynchronizationEnabled
        {
            get { return _statusSynchronizationEnabled; }
            set { _statusSynchronizationEnabled = value; }
        }

        volatile IOperational _statusSynchronizationSource = null;
        
        /// <summary>
        /// If the current unit must synchronize status with a given source, set this variable
        /// and the synchronization is done automatically.
        /// </summary>
        protected IOperational StatusSynchronizationSource
        {
            get { return _statusSynchronizationSource; }
            set 
            {
                if (_statusSynchronizationSource != null)
                {
                    _statusSynchronizationSource.OperationalStateChangedEvent -= new OperationalStateChangedDelegate(_statusSynchronizationSource_OperationalStatusChangedEvent);
                    _statusSynchronizationSource = null;
                }

                _statusSynchronizationSource = value;

                if (_statusSynchronizationSource != null)
                {
                    _statusSynchronizationSource.OperationalStateChangedEvent += new OperationalStateChangedDelegate(_statusSynchronizationSource_OperationalStatusChangedEvent);

                    if (_operationalState != _statusSynchronizationSource.OperationalState)
                    {
                        ChangeOperationalState(_statusSynchronizationSource.OperationalState);
                    }
                }
            }
        }

        [field: NonSerialized]
        public event OperationalStateChangedDelegate OperationalStateChangedEvent;

        /// <summary>
        /// Constructor.
        /// </summary>
        public Operational()
        {
        }

        public virtual void OnDeserialization(object sender)
        {
            StatusSynchronizationSource = _statusSynchronizationSource;
        }

        /// <summary>
        /// Change the component operational state.
        /// </summary>
        /// <param name="operationalState"></param>
        protected virtual void ChangeOperationalState(OperationalStateEnum operationalState)
        {
            OperationalStateEnum previousState;
            lock (this)
            {
                if (operationalState == _operationalState)
                {
                    return;
                }

                previousState = _operationalState;
            }

#if Matrix_Diagnostics
            SystemMonitor.Debug(this.GetType().Name + " was " + previousState.ToString() + " is now " + operationalState.ToString());
#endif

            _operationalState = operationalState;
            if (OperationalStateChangedEvent != null)
            {
                OperationalStateChangedEvent(this, previousState);
            }
        }

        /// <summary>
        /// Follow synchrnozation source status.
        /// </summary>
        void _statusSynchronizationSource_OperationalStatusChangedEvent(IOperational parameter1, OperationalStateEnum parameter2)
        {
            if (StatusSynchronizationEnabled)
            {
#if Matrix_Diagnostics
                SystemMonitor.Debug(this.GetType().Name + " is following its source " + parameter1.GetType().Name + " to state " + parameter1.OperationalState.ToString());
#endif
                this.ChangeOperationalState(parameter1.OperationalState);
            }
            else
            {
#if Matrix_Diagnostics
                SystemMonitor.Debug(this.GetType().Name + " is not following its source " + parameter1.GetType().Name + " to new state because synchronization is disabled.");
#endif
            }
        }

        /// <summary>
        /// Raise event helper.
        /// </summary>
        /// <param name="previousState"></param>
        protected void RaiseOperationalStatusChangedEvent(OperationalStateEnum previousState)
        {
            if (OperationalStateChangedEvent != null)
            {
                OperationalStateChangedEvent(this, previousState);
            }
        }

        /// <summary>
        /// Helper.
        /// </summary>
        public static bool IsInitOrOperational(OperationalStateEnum state)
        {
            return state == OperationalStateEnum.Initialized || state == OperationalStateEnum.Operational;
        }

    }
}
