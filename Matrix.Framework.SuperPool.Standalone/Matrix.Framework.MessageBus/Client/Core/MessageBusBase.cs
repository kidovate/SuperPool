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
using Matrix.Common.Extended.ThreadPools;
using Matrix.Framework.MessageBus.Net;
using Matrix.Framework.MessageBus.Clients.ExecutionStrategies;

#if Matrix_Diagnostics
    using Matrix.Common.Diagnostics;
#endif

namespace Matrix.Framework.MessageBus.Core
{
    /// <summary>
    /// Base class for implementing a message bus.
    /// </summary>
    public abstract class MessageBusBase : IMessageBus
    {
        volatile bool _isDisposed = false;

        protected bool IsDisposed
        {
            get { return _isDisposed; }
        }

#if Matrix_Diagnostics
        private InstanceMonitor _monitor;
        protected InstanceMonitor InstanceMonitor
        {
            get { return _monitor; }
        }
#endif

        volatile string _name = string.Empty;
        /// <summary>
        /// Name of the bus.
        /// </summary>
        public string Name
        {
            get { return _name; }
        }

        volatile ThreadPoolFastEx _defaultThreadPool;
        /// <summary>
        /// The pool this bus uses by default.
        /// </summary>
        public ThreadPoolFastEx DefaultThreadPool
        {
            get { return _defaultThreadPool; }
        }

        protected enum SendToClientResultEnum
        {
            ClientNotFound,
            Failure,
            Success
        }

        public event MessageBusClientUpdateDelegate ClientAddedEvent;
        public event MessageBusClientRemovedDelegate ClientRemovedEvent;
        public event MessageBusClientUpdateDelegate ClientUpdateEvent;
        public event MessageBusCounterPartyUpdateDelegate CounterPartyUpdateEvent;

        /// <summary>
        /// Constructor.
        /// </summary>
        public MessageBusBase(string name)
        {
            _name = name;

#if Matrix_Diagnostics
            _monitor = new InstanceMonitor(this);
#endif
            
            _defaultThreadPool = new ThreadPoolFastEx(name + ".DefaultThreadPool");

            _defaultThreadPool.MinimumThreadsCount = 5;
            _defaultThreadPool.MaximumThreadsCount = 10;
        }

        #region IDisposable Members

        public virtual void Dispose()
        {
            _isDisposed = true;

            ThreadPoolFastEx threadPool = _defaultThreadPool;
            if (threadPool != null)
            {
                threadPool.Dispose();
                _defaultThreadPool = null;
            }
        }

        #endregion

        public abstract bool AddClient(MessageBusClient client);
        public abstract bool RemoveClient(MessageBusClient client, bool isPermanent);
        public abstract bool ContainsClient(ClientId client);

        public abstract Type GetClientType(ClientId clientId);
        public abstract List<string> GetClientSourceTypes(ClientId clientId);

        public abstract List<ClientId> GetAllClientsIds();

        /// <summary>
        /// Obtain an instance of the client, in case it is a local client
        /// and the instance is accessible. This will not work for remote
        /// (network) associated clients.
        /// </summary>
        public abstract MessageBusClient GetLocalClientInstance(ClientId clientId);

        protected void RaiseClientAddedEvent(ClientId clientId)
        {
            MessageBusClientUpdateDelegate delegateInstance = ClientAddedEvent;
            if (delegateInstance != null)
            {
                delegateInstance(this, clientId);
            }
        }

        protected void RaiseClientRemovedEvent(ClientId clientId, bool isPermanent)
        {
            MessageBusClientRemovedDelegate delegateInstance = ClientRemovedEvent;
            if (delegateInstance != null)
            {
                delegateInstance(this, clientId, isPermanent);
            }
        }

        protected void RaiseClientUpdateEvent(ClientId clientId)
        {
            MessageBusClientUpdateDelegate delegateInstance = ClientUpdateEvent;
            if (delegateInstance != null)
            {
                delegateInstance(this, clientId);
            }
        }

        protected void RaiseCounterPartyUpdateEvent(string partyId, string state)
        {
            MessageBusCounterPartyUpdateDelegate del = CounterPartyUpdateEvent;
            if (del != null)
            {
                del(this, partyId, state);
            }
        }

        
        /// <summary>
        /// Send a responce to an envelope received.
        /// </summary>
        /// <param name="receivedEnvelope">The enveloped received, we shall respond to it.</param>
        /// <param name="envelope">The new envelope, we are sending as responce.</param>
        /// <returns></returns>
        public Outcomes Respond(Envelope receivedEnvelope, Envelope envelope)
        {
            envelope.Address = EnvelopeTransportation.CreateResponseTransport(receivedEnvelope.History);
            return Send(envelope);
        }

        /// <summary>
        /// Addressed envelope.
        /// </summary>
        protected Outcomes Send(Envelope envelope)
        {
            if (envelope.Address != null)
            {
                EnvelopeStamp stamp = envelope.Address.PopStamp();
                if (stamp != null)
                {
                    return Send(stamp.SenderIndex, stamp.ReceiverIndex, envelope, null, false);
                }
            }

            return Outcomes.Failure;
        }

        /// <summary>
        /// Send an item to a single repicient.
        /// </summary>
        /// <returns></returns>
        public Outcomes Send(ClientId senderId, ClientId receiverId, Envelope envelope, TimeSpan? requestConfirmTimeout, bool showErrorsDiagnostics)
        {
            return Send(senderId, new ClientId[] { receiverId }, envelope, requestConfirmTimeout, showErrorsDiagnostics);
        }

        /// <summary>
        /// Send an item to multiple recipients.
        /// </summary>
        public Outcomes Send(ClientId senderId, IEnumerable<ClientId> receiversIds, Envelope envelope, TimeSpan? requestConfirmTimeout, bool showErrorsDiagnostics)
        {
            return DoSend(senderId, receiversIds, envelope, requestConfirmTimeout, showErrorsDiagnostics);
        }

        protected abstract Outcomes DoSend(ClientId senderIndex, IEnumerable<ClientId> receiversIndeces, Envelope envelope, TimeSpan? requestConfirmTimeout, bool showErrorsDiagnostics);

    
    }
}
