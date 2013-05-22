// -----
// Copyright 2010 Deyan Timnev
// This file is part of the Matrix Platform (www.matrixplatform.com).
// The Matrix Platform is free software: you can redistribute it and/or modify it under the terms of the GNU Lesser General Public License as published by the Free Software Foundation, 
// either version 3 of the License, or (at your option) any later version. The Matrix Platform is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; 
// without even the implied warranty of  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Lesser General Public License for more details.
// You should have received a copy of the GNU Lesser General Public License along with the Matrix Platform. If not, see http://www.gnu.org/licenses/lgpl.html
// -----
using System;
using Matrix.Framework.MessageBus.Clients.ExecutionStrategies;
using Matrix.Framework.MessageBus.Core;
using Matrix.Common.Core.Identification;

namespace Matrix.Framework.MessageBus.Clients
{
    /// <summary>
    /// Default stub standalone implementation of an Message bus Slim client.
    /// </summary>
    public class ActiveClient : MessageBusClient
    {
        volatile IMessageBus _messageBus = null;
        /// <summary>
        /// The instance of the message bus this client belongs to.
        /// </summary>
        public override IMessageBus MessageBus
        {
            get { return _messageBus; }
        }

        ExecutionStrategy _executionStrategy;
        /// <summary>
        /// Execution strategy defines how threads are managed on executing the tasks in the client.
        /// </summary>
        public override ExecutionStrategy ExecutionStrategy
        {
            get { return _executionStrategy; }
        }

        ClientId _id;
        /// <summary>
        /// The Id of this client as provided by the currently assigned
        /// message bus.
        /// </summary>
        public override ClientId Id
        {
            get { return _id; }
        }

        public override Type OptionalSourceType
        {
            get { return null; }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public ActiveClient(ClientId id)
        {
            _id = id;

            _executionStrategy = null;
            _messageBus = null;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public ActiveClient(string name)
            : this(new ClientId(name))
        {
        }

        public override void Dispose()
        {
            _executionStrategy = null;
            _messageBus = null;

            base.Dispose();
        }

        /// <summary>
        /// Setup execution strategy for the 
        /// execution of class on this client.
        /// </summary>
        public override bool SetupExecutionStrategy(ExecutionStrategy executionStrategy)
        {
            lock (this)
            {
                if (_executionStrategy != null)
                {
                    _executionStrategy.Dispose();
                    _executionStrategy = null;
                }

                _executionStrategy = executionStrategy;
            }

            if (executionStrategy != null)
            {
                return executionStrategy.Initialize(this);
            }

            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        internal override bool AssignMessageBus(IMessageBus messageBus, int indexId)
        {
            if (_messageBus != null)
            {
                return false;
            }

            _id.MessageBus = messageBus;
            _id.LocalMessageBusIndex = indexId;

            //lock (this)
            {// Lock needed to assure operations go together.
                _messageBus = messageBus;
            }

            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        internal override void ReleaseMessageBus()
        {
            lock (this)
            {// Lock needed to assure operations go together.
                _messageBus = null;
            }
        }

        /// <summary>
        /// Receive an envelope for executing.
        /// </summary>
        internal override bool Receive(Envelope envelope)
        {
            ExecutionStrategy executionStrategy = _executionStrategy;
            if (executionStrategy != null)
            {
                executionStrategy.Execute(envelope);
            }

            RaiseEnvelopeReceivedEvent(envelope);
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual void PerformExecution(Envelope envelope)
        {
            RaiseEnvelopeExecutingEvent(envelope);

            OnPerformExecution(envelope);

            RaiseEnvelopeExecutedEvent(envelope);
        }

        /// <summary>
        /// 
        /// </summary>
        protected virtual void OnPerformExecution(Envelope envelope)
        {

        }

    }
}
