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

namespace Matrix.Framework.MessageBus.Core
{
    /// <summary>
    /// Helper delegate.
    /// </summary>
    public delegate void MessageBusClientUpdateDelegate(IMessageBus messageBus, ClientId clientId);

    /// <summary>
    /// Helper delegate.
    /// </summary>
    /// <param name="permanentRemove">Clients may be temprorarily removed (for ex. when temporarily offline) or permanently. Permanent means destroy all possible and pending connections to client.</param>
    public delegate void MessageBusClientRemovedDelegate(IMessageBus messageBus, ClientId clientId, bool permanentRemove);

    /// <summary>
    /// Helper delegate.
    /// </summary>
    /// <param name="permanentRemove">Clients may be temprorarily removed (for ex. when temporarily offline) or permanently. Permanent means destroy all possible and pending connections to client.</param>
    public delegate void MessageBusCounterPartyUpdateDelegate(IMessageBus messageBus, string counterPartyId, string state);

    /// <summary>
    /// Provides information on the outcome of a send operation.
    /// </summary>
    public enum Outcomes
    {
        // There is no information if the call succeeded or not.
        Unknown,
        // Call succeeded.
        Success,
        // Call failed, reason not specified.
        Failure,
        // Call failed due to time out.
        TimeoutFailure,
        // System is not able to process the operation.
        SystemFailture
    }

    /// <summary>
    /// Interface defines the appearance of a message bus.
    /// </summary>
    public interface IMessageBus : IDisposable
    {
        /// <summary>
        /// Raised when a client has been added to the bus.
        /// </summary>
        event MessageBusClientUpdateDelegate ClientAddedEvent;
        
        /// <summary>
        /// Raised when the client has been removed from the bus.
        /// Clients may be temprorarily removed (for ex. when temporarily offline) or permanently. Permanent means destroy all possible and pending connections to client.
        /// </summary>
        event MessageBusClientRemovedDelegate ClientRemovedEvent;
        
        /// <summary>
        /// Raise when a client has updated it self, and wishes to share this knowledge.
        /// This is not message bus specific, just a helper path to allow items to
        /// minitor each other for common changes withough a complex message mechanism.
        /// </summary>
        event MessageBusClientUpdateDelegate ClientUpdateEvent;

        /// <summary>
        /// If the message bus is connected to some other counter party (for ex. a 
        /// server or a client mesasge bus) and this party changes state (for ex.
        /// gets shutdown), this is the notification.
        /// </summary>
        event MessageBusCounterPartyUpdateDelegate CounterPartyUpdateEvent;

        /// <summary>
        /// Name of the message bus (for ex. assign something descriptive of 
        /// the module and operations the bus is to be performing).
        /// </summary>
        string Name { get; }

        /// <summary>
        /// The deafault thread pool instance that is used to execute items on the bus.
        /// </summary>
        ThreadPoolFastEx DefaultThreadPool { get; }

        /// <summary>
        /// Add a client to the message bus.
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        bool AddClient(MessageBusClient client);
        
        /// <summary>
        /// Remove a client from the message bus.
        /// </summary>
        /// <param name="client"></param>
        /// <param name="isPermanent"></param>
        /// <returns></returns>
        bool RemoveClient(MessageBusClient client, bool isPermanent);
        
        /// <summary>
        /// Check if a client with this ID is part of this message bus.
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        bool ContainsClient(ClientId client);

        /// <summary>
        /// Obtain the Ids of all clients.
        /// </summary>
        /// <returns></returns>
        List<ClientId> GetAllClientsIds();

        /// <summary>
        /// Directly obtain the instance of a local client, use with caution, 
        /// since this brakes the decoupling model, and must be utilized only
        /// when absolutely necessary.
        /// </summary>
        /// <param name="clientId"></param>
        /// <returns></returns>
        MessageBusClient GetLocalClientInstance(ClientId clientId);

        /// <summary>
        /// Obtain the type of the client instance. This is NOT 
        /// the type of the source, only the type of the client.
        /// </summary>
        /// <param name="clientId"></param>
        /// <returns></returns>
        Type GetClientType(ClientId clientId);
        
        /// <summary>
        /// Get the (assembly qualified) names of the client source;
        /// this provides not only the actual source type, but also 
        /// all the classes and interfaces it inherits, since the
        /// discovery process needs that to consume common interfaces
        /// with separate implementations.
        /// </summary>
        List<string> GetClientSourceTypes(ClientId clientId);

        /// <summary>
        /// Send a message.
        /// </summary>
        /// <param name="senderId">The identified of the sending party.</param>
        /// <param name="receiversIds">The identifier of the receiving party.</param>
        /// <param name="envelope">The envelope containing the message sent.</param>
        /// <param name="requestConfirmTimeout">Timeout assigned for the "confirmation of receival" of the envelope. Not related in any way to the execution of the message.</param>
        /// <param name="showErrorsDiagnostics">Show errors in diagnostics.</param>
        /// <returns>True if the call was a success, otherwise false.</returns>
        Outcomes Send(ClientId senderId, ClientId receiverId, Envelope envelope, TimeSpan? requestConfirmTimeout, bool showErrorsDiagnostics);
        
        /// <summary>
        /// Send a message.
        /// </summary>
        /// <param name="senderId">The identified of the sending party.</param>
        /// <param name="receiversIds">The identifiers of the receiving parties.</param>
        /// <param name="envelope">The envelope containing the message sent.</param>
        /// <param name="requestConfirmTimeout">Timeout assigned for the "confirmation of receival" of the envelope. Not related in any way to the execution of the message.</param>
        /// <param name="showErrorsDiagnostics">Show errors in diagnostics.</param>
        /// <returns>True if the call was a success, otherwise false.</returns>
        Outcomes Send(ClientId senderId, IEnumerable<ClientId> receiversIds, Envelope envelope, TimeSpan? requestConfirmTimeout, bool showErrorsDiagnostics);
        
        /// <summary>
        /// Respond to a received message.
        /// </summary>
        /// <param name="receivedEnvelope">The envelope containing the message received.</param>
        /// <param name="responseEnvelope">The responding envelope.</param>
        /// <returns>True if the call was a success, otherwise false.</returns>
        Outcomes Respond(Envelope receivedEnvelope, Envelope responseEnvelope);
    }
}
