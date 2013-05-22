// -----
// Copyright 2010 Deyan Timnev
// This file is part of the Matrix Platform (www.matrixplatform.com).
// The Matrix Platform is free software: you can redistribute it and/or modify it under the terms of the GNU Lesser General Public License as published by the Free Software Foundation, 
// either version 3 of the License, or (at your option) any later version. The Matrix Platform is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; 
// without even the implied warranty of  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Lesser General Public License for more details.
// You should have received a copy of the GNU Lesser General Public License along with the Matrix Platform. If not, see http://www.gnu.org/licenses/lgpl.html
// -----
using System;
using Matrix.Common.Extended.Operationals;
using Matrix.Framework.MessageBus.Clients.ExecutionStrategies;

namespace Matrix.Framework.MessageBus.Core
{
    /// <summary>
    /// Base interface for client implementations of the message bus client.
    /// </summary>
    public abstract class MessageBusClient : Operational, IDisposable
    {
        public abstract IMessageBus MessageBus { get; }

        public abstract ClientId Id { get; }

        public abstract ExecutionStrategy ExecutionStrategy { get; }

        /// <summary>
        /// The type of the source (optional, may be null in case client is used in standalone mode).
        /// </summary>
        public abstract Type OptionalSourceType { get; }

        public delegate void EnvelopeUpdateDelegate(MessageBusClient stub, Envelope envelope);
        public delegate void ClientUpdateDelegate(MessageBusClient client);

        /// <summary>
        /// *IMPORTANT* this event is executed on an incoming thread from the message bus, and must never be blocked, since it will compomise the model.
        /// This is NOT executed on the new execution thread, use EnvelopeExecuteEvent for this. Process calls on this event as fast as possible.
        /// </summary>
        public event EnvelopeUpdateDelegate EnvelopeReceivedEvent;

        public event EnvelopeUpdateDelegate EnvelopeExecutingEvent;
        public event EnvelopeUpdateDelegate EnvelopeExecutedEvent;

        /// <summary>
        /// General update of some of the properties of the client.
        /// </summary>
        public event ClientUpdateDelegate UpdateEvent;

        /// <summary>
        /// Constructor.
        /// </summary>
        public MessageBusClient()
        {
        }

        /// <summary>
        /// Dispose of any associations or resources.
        /// Will also free any of the events, although it 
        /// is any subscriber responcibility to release the events as well.
        /// </summary>
        public virtual void Dispose()
        {
            EnvelopeReceivedEvent = null;
            EnvelopeExecutingEvent = null;
            EnvelopeExecutedEvent = null;
            UpdateEvent = null;
        }

        internal abstract bool Receive(Envelope envelope);

        internal abstract bool AssignMessageBus(IMessageBus messageBus, int indexId);

        internal abstract void ReleaseMessageBus();

        public abstract bool SetupExecutionStrategy(ExecutionStrategy executionStrategy);
        
        protected void RaiseEnvelopeReceivedEvent(Envelope envelope)
        {
            EnvelopeUpdateDelegate envelopeReceivedDelegate = EnvelopeReceivedEvent;
            if (envelopeReceivedDelegate != null)
            {
                envelopeReceivedDelegate(this, envelope);
            }
        }

        protected void RaiseEnvelopeExecutingEvent(Envelope envelope)
        {
            EnvelopeUpdateDelegate envelopeReceivedDelegate = EnvelopeExecutingEvent;
            if (envelopeReceivedDelegate != null)
            {
                envelopeReceivedDelegate(this, envelope);
            }
        }

        protected void RaiseEnvelopeExecutedEvent(Envelope envelope)
        {
            EnvelopeUpdateDelegate envelopeReceivedDelegate = EnvelopeExecutedEvent;
            if (envelopeReceivedDelegate != null)
            {
                envelopeReceivedDelegate(this, envelope);
            }
        }

        /// <summary>
        /// Notify all interested parties, that the client has undergone some update.
        /// </summary>
        public void RaiseUpdateEvent()
        {
            ClientUpdateDelegate del = UpdateEvent;
            if (del != null)
            {
                del(this);
            }
        }
    }
}
