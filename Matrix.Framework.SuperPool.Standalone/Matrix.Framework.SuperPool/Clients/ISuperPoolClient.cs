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
using Matrix.Framework.SuperPool.Core;
using Matrix.Framework.SuperPool.Subscription;
using Matrix.Common.Core.Identification;

namespace Matrix.Framework.SuperPool.Clients
{
    /// <summary>
    /// Delegate used when receiving asynchronous results.
    /// </summary>
    public delegate void AsyncCallResultDelegate(ISuperPoolClient client, AsyncResultParams param);

    /// <summary>
    /// Helper delegates, used when the source object of the client has been updated.
    /// </summary>
    public delegate void SuperPoolSourceUpdateDelegate(ISuperPoolClient client, object oldSource, object newSource);

    /// <summary>
    /// Helper delegate, used in events that signify update in the state of a super pool client.
    /// </summary>
    /// <param name="client"></param>
    public delegate void SuperPoolClientUpdateDelegate(ISuperPoolClient client);

    /// <summary>
    /// Interface defines the basic appearance of a super pool client.
    /// 
    /// *Note* all parameters and results are (by default) transported by reference.
    /// Serialization is only applied when transporting over TCP/IP boundary.
    /// </summary>
    public interface ISuperPoolClient
    {
        /// <summary>
        /// Identification information of this client.
        /// </summary>
        ClientId Id { get; }

        /// <summary>
        /// The super pool instance this client is assigned to (may be null).
        /// </summary>
        Matrix.Framework.SuperPool.Core.SuperPool SuperPool { get; }
        
        /// <summary>
        /// The source used for integrating the automated operations of the client.
        /// </summary>
        object Source { get; set; }

        /// <summary>
        /// Event raised when a source of this client has changed.
        /// </summary>
        event SuperPoolSourceUpdateDelegate SourceUpdatedEvent;

        #region Call Methods

        /// <summary>
        /// Async call to a single receiver.
        /// </summary>
        /// <returns>NA (Default value for this type, for ex. 0 for integer, null for class etc.; not actual result).</returns>
        TType Call<TType>(ComponentId receiverId)
            where TType : class;

        /// <summary>
        /// Call and receive any result that may come with the usage of asyncDelegate. 
        /// </summary>
        /// <returns>NA (Default value for this type, for ex. 0 for integer, null for class etc.; not actual result).</returns>
        TType Call<TType>(ComponentId receiverId, AsyncCallResultDelegate asyncDelegate)
            where TType : class;

        /// <summary>
        /// Call and receive any result that may come with the usage of asyncDelegate; use state to 
        /// send any call identification information you may wish to send.
        /// </summary>
        /// <returns>NA (Default value for this type, for ex. 0 for integer, null for class etc.; not actual result).</returns>
        TType Call<TType>(ComponentId receiverId, AsyncCallResultDelegate asyncDelegate, object state)
            where TType : class;

        /// <summary>
        /// Async call to multiple receivers. Execution may be concurrent.
        /// </summary>
        /// <returns>NA (Default value for this type, for ex. 0 for integer, null for class etc.; not actual result).</returns>
        TType Call<TType>(IEnumerable<ComponentId> receivers)
            where TType : class;

        /// <summary>
        /// Async call to the first recipient found that implements this (TType) service.
        /// Typically local components are provided before remote ones.
        /// </summary>
        /// <returns>NA (Default value for this type, for ex. 0 for integer, null for class etc.; not actual result).</returns>
        TType CallFirst<TType>()
            where TType : class;

        /// <summary>
        /// Synchronous call operation, to a specific receiver.
        /// This will (try to) perform a synchronous call and wait for the result (if there is one).
        /// Default timeout is used.
        /// </summary>
        /// <returns>Actual result returned by the call, if performed successfully in time, otherwise a default value for this type, for ex. 0 for integer, null for class etc.</returns>
        TType CallSync<TType>(ComponentId receiverId)
            where TType : class;

        /// <summary>
        /// Synchronous call operation.
        /// This will (try to) perform a synchronous call and wait for the result (if there is one).
        /// </summary>
        /// <returns>Actual result returned by the call, if performed successfully in time, otherwise a default value for this type, for ex. 0 for integer, null for class etc.</returns>
        TType CallSync<TType>(ComponentId receiverId, TimeSpan timeOut)
            where TType : class;

        /// <summary>
        /// Synchronous call operation, with no specified receiver, 
        /// will try to find one (the first) provider and execute upon it.
        /// </summary>
        /// <returns>Actual result returned by the call, if performed successfully in time, otherwise a default value for this type, for ex. 0 for integer, null for class etc.</returns>
        TType CallSyncFirst<TType>(TimeSpan timeOut)
            where TType : class;

        /// <summary>
        /// A third option, to the Call and CallSync, 
        /// this method offers a little of both worlds.
        /// 
        /// Do a call, and wait for a response of the receiver 
        /// that it *actually received the call*. It will *not wait* or 
        /// establish *the result*, nor will it wait for the result to be
        /// generated, only make sure the caller has received the call.
        /// </summary>
        /// <param name="receiverId">The Identifier of the receiver.</param>
        /// <param name="confirmationTimeout">How long to wait for a confirmation for.</param>
        /// <param name="outcome">Stores info on the outcome of the call, use if need to know if call failed.</param>
        /// <typeparam name="TType">Interface type to perform call upon.</typeparam>
        /// <returns>NA (Default value for this type, for ex. 0 for integer, null for class etc.; not actual result).</returns>
        TType CallConfirmed<TType>(ComponentId receiverId, TimeSpan? confirmationTimeout, out CallOutcome outcome)
            where TType : class;

        /// <summary>
        /// Call and receive any result that may come with the usage of asyncDelegate; use state to 
        /// track any call identification information you may wish to use in the callback.
        /// 
        /// Since in this version no direct receiver is identified, make sure to specify a maximum number of
        /// results to accept, as well as how long to wait for results to come.
        /// </summary>
        /// <param name="resultWaitTimeout">How long to wait for results coming in.</param>
        /// <returns>NA (Default value for this type, for ex. 0 for integer, null for class etc.; not actual result).</returns>
        TType CallAll<TType>(AsyncCallResultDelegate asyncDelegate, TimeSpan asyncResultTimeout)
            where TType : class;
        
        /// <summary>
        /// For details see previous version:
        /// Call{TType}(AsyncCallResultDelegate asyncDelegate, TimeSpan asyncResultTimeout).
        /// </summary>
        /// <param name="asyncResultTimeout">How long to wait for results coming in.</param>
        /// <param name="state">Use this to pass a custom tag to trace this specific call.</param>
        /// <returns>NA (Default value for this type, for ex. 0 for integer, null for class etc.; not actual result).</returns>
        TType CallAll<TType>(AsyncCallResultDelegate asyncDelegate, object state, TimeSpan asyncResultTimeout)
            where TType : class;

        /// <summary>
        /// Async call with no receiver, means call all available recipients 
        /// of this interface and method.
        /// </summary>
        /// <returns>NA (Default value for this type, for ex. 0 for integer, null for class etc.; not actual result).</returns>
        TType CallAll<TType>()
            where TType : class;

        /// <summary>
        /// Advanced mode!! Perform a direct (fully synchronous) call, only applicable 
        /// to receiver that is a client of the *local super pool instance*.
        /// 
        /// *Important* this is a direct call, and it will arrive and execute
        /// instantly, on the invocating thread; as a result it may end up being 
        /// executed before previously sent standard Calls(), since they are usually
        /// delayed a few ms before a thread is available to execute them.
        /// 
        /// There is no time out mechanism in this type of call, so it will take
        /// as long as the call takes.
        /// </summary>
        /// <returns>Always the actual result of the call.</returns>
        TType CallDirectLocal<TType>(ComponentId receiver)
            where TType : class;

        #endregion

        #region Subscribe methods

        /// <summary>
        /// Subscribe to all events of this type, 
        /// that may arise from any single component.
        /// </summary>
        TType SubscribeAll<TType>()
            where TType : class;

        /// <summary>
        /// Subscribe to event on the given source.
        /// </summary>
        TType Subscribe<TType>(ComponentId sourceId)
            where TType : class;


        /// <summary>
        /// Perform an event subscription.
        /// All subscribes are expected to be asynchronous, 
        /// and executed against the actual pool only.
        /// </summary>
        TType Subscribe<TType>(EventSubscriptionRequest subscription)
            where TType : class;


        #endregion

        #region Service discovery

        /// <summary>
        /// Obtain the Id of the first element that implements the provided (TInterfaceType) interface.
        /// </summary>
        /// <typeparam name="TType"></typeparam>
        /// <returns></returns>
        ClientId Resolve<TInterfaceType>();

        /// <summary>
        /// Obtain the Id of the first element that implements the provided (TInterfaceType) interface.
        /// </summary>
        /// <typeparam name="TType"></typeparam>
        /// <returns></returns>
        ClientId Resolve(Type interfaceType);

        /// <summary>
        /// Obtain the Id of all the elements that implements the (TInterfaceType) interface.
        /// </summary>
        /// <typeparam name="TType"></typeparam>
        /// <returns></returns>
        List<ClientId> ResolveAll<TInterfaceType>();

        /// <summary>
        /// Obtain the Id of all the elements that implements the (TInterfaceType) interface.
        /// </summary>
        /// <typeparam name="TType"></typeparam>
        /// <returns></returns>
        List<ClientId> ResolveAll(Type interfaceType);

        #endregion
    }
}
