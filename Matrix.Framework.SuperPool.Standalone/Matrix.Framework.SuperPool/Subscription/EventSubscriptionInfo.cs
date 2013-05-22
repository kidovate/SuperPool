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
using System.Reflection;
using Matrix.Common.Core.Collections;
using Matrix.Framework.MessageBus.Core;
using System.Collections.ObjectModel;

#if Matrix_Diagnostics
using Matrix.Common.Diagnostics;
#endif

namespace Matrix.Framework.SuperPool.Subscription
{
    /// <summary>
    /// Class contains information on event (item) subscriptions inside the super pool.
    /// </summary>
    internal class EventSubscriptionInfo : IDisposable
    {
        string _extendedEventName = string.Empty;

        // This is used as a special value of "-1" to indicate a subscribe for any source raise of this event.
        ClientId SubscribeToAllId = new ClientId("SubscribeToAll");

        /// <summary>
        /// Store info for the subscriptions related to a single source; 
        /// the Receivers holds all the receivers of the current even, when raised from the source.
        /// </summary>
        internal class ClientEventSubscriptionInfo
        {
            /// <summary>
            /// Receiver Id vs Receiver MethodInfo
            /// </summary>
            internal HotSwapDictionary<MethodInfo, int> Data { get; set; }

            /// <summary>
            /// Constructor.
            /// </summary>
            internal ClientEventSubscriptionInfo()
            {
                Data = new HotSwapDictionary<MethodInfo,int>();
            }

            internal void Update(MethodInfo methodInfo, bool addSubscription, int? specificValue)
            {
                lock(this)
                {
                    if (Data.ContainsKey(methodInfo) == false)
                    {
                        Data.Add(new KeyValuePair<MethodInfo,int>(methodInfo, 0));
                    }

                    if (specificValue.HasValue)
                    {
                        Data[methodInfo] = specificValue.Value;
                    }
                    else
                    {
                        if (addSubscription)
                        {
                            Data[methodInfo] = Data[methodInfo] + 1;
                        }
                        else
                        {
                            Data[methodInfo] = Math.Max(0, Data[methodInfo] - 1);
                        }
                    }
                }
            }

        }

        /// <summary>
        /// Source(sender, generator) id (vs) List of clients (receivers) subscriptions, each with its MethodInfo for the accepting method and Id for the accepting client.
        /// The hot swap properties of both these items are reused multiple times, so replacement with conventional dictionary/list not adviseable.
        /// 
        /// There is also a special value of SubscribeToAllCode to indicate a subscribe for any source raise of this event.
        /// </summary>
        HotSwapDictionary<ClientId, HotSwapDictionary<ClientId, ClientEventSubscriptionInfo>> _subscriptionsHotSwap = new HotSwapDictionary<ClientId, HotSwapDictionary<ClientId, ClientEventSubscriptionInfo>>();

        /// <summary>
        /// Constructor.
        /// </summary>
        public EventSubscriptionInfo(string extendedEventName)
        {
            _extendedEventName = extendedEventName;
        }

        /// <summary>
        /// 
        /// </summary>
        public void Dispose()
        {
            _subscriptionsHotSwap.Clear();
        }

        /// <summary>
        /// Remove any subscriptions this client may have.
        /// </summary>
        /// <param name="client"></param>
        public void RemoveClientSubscriptions(ClientId clientId)
        {
            foreach (KeyValuePair<ClientId, HotSwapDictionary<ClientId, ClientEventSubscriptionInfo>> pair in _subscriptionsHotSwap)
            {
                pair.Value.Remove(clientId);
            }

            //foreach (HotSwapList<KeyValuePair<MessageBusClientId, MethodInfo>> list in _subscriptionsHotSwap.Values)
            //{
            //    foreach (KeyValuePair<MessageBusClientId, MethodInfo> pair in list)
            //    {
            //        if (pair.Key.Equals(clientId))
            //        {// *WARNING*, hot swap specific operation (remove while iterating), will not work properly on non-hot swap lists. 
            //            list.Remove(pair);
            //        }
            //    }
            //}
        }

        /// <summary>
        /// Obtain a list of the receivers of the event raise.
        /// </summary>
        /// <param name="specific">True to get the specific for this one, false to get the "subscribe to all"</param>
        public HotSwapDictionary<ClientId, ClientEventSubscriptionInfo> GetReceivers(ClientId sourceId, bool specific)
        {
            HotSwapDictionary<ClientId, ClientEventSubscriptionInfo> result;
            if (specific)
            {
                if (_subscriptionsHotSwap.TryGetValue(sourceId, out result) == false)
                {// If not found, return an empty one.
                    result = new HotSwapDictionary<ClientId, ClientEventSubscriptionInfo>();
                }
            }
            else
            {
                if (_subscriptionsHotSwap.TryGetValue(SubscribeToAllId, out result) == false)
                {// If not found, return an empty one.
                    result = new HotSwapDictionary<ClientId, ClientEventSubscriptionInfo>();
                }
            }

            return result;
        }

        /// <summary>
        /// Gather a lsit of update requests, related to a specific target.
        /// </summary>
        /// <returns></returns>
        public List<EventSubscriptionRequest> GatherSourceRelatedUpdates(ClientId sourceId)
        {
            List<EventSubscriptionRequest> result = new List<EventSubscriptionRequest>();

            HotSwapDictionary<ClientId, ClientEventSubscriptionInfo> values;
            if (_subscriptionsHotSwap.TryGetValue(sourceId, out values))
            {

                foreach (KeyValuePair<ClientId, ClientEventSubscriptionInfo> pair in values)
                {
                    foreach (KeyValuePair<MethodInfo, int> subPair in pair.Value.Data)
                    {
                        EventSubscriptionRequest request = new EventSubscriptionRequest(sourceId);
                        request.DelegateInstanceMethodInfo = subPair.Key;
                        request.SpecificCountOptional = subPair.Value;
                        request.ExtendedEventName = _extendedEventName;

                        request.SenderId = pair.Key;
                        request.IsAdd = true;

                        result.Add(request);
                    }
                }
            }

            //foreach (KeyValuePair<MessageBusClientId, HotSwapDictionary<MessageBusClientId, ClientEventSubscriptionInfo>> pair
            //    in _subscriptionsHotSwap)
            //{
            //    ClientEventSubscriptionInfo info;
            //    if (pair.Value.TryGetValue(sourceId, out info))
            //    {
            //        foreach (KeyValuePair<MethodInfo, int> subPair in info.Data)
            //        {
            //            EventSubscriptionRequest request = new EventSubscriptionRequest(sourceId);
            //            request.DelegateInstanceMethodInfo = subPair.Key;
            //            request.SpecificCountOptional = subPair.Value;
            //            request.ExtendedEventName = _extendedEventName;

            //            request.SenderId = pair.Key;
            //            request.IsAdd = true;

            //            result.Add(request);
            //        }
            //    }
            //}

            return result;
        }

        /// <summary>
        /// Apply an update of the subscription structute on current event, based on request data.
        /// </summary>
        public void SubscriptionUpdate(EventSubscriptionRequest request)
        {
            if (request.SenderId == null)
            {
#if Matrix_Diagnostics
                SystemMonitor.OperationError("Proxy call or proxy call sender not found in a super pool subscription update.");
#endif
                return;
            }

            if (request == null)
            {
#if Matrix_Diagnostics
                SystemMonitor.Error(string.Format("Subscription request not available, subscription failed, delegate method [{0}].", request.DelegateInstanceMethodInfo.ToString()));
#endif
                return;
            }

            ReadOnlyCollection<ClientId> sources = request.EventsSources;
            if (sources != null && sources.Count > 0)
            {
                foreach (ClientId id in sources)
                {
                    DoUpdateSubscription(request.SenderId, request.DelegateInstanceMethodInfo, id, request.IsAdd, request.SpecificCountOptional);
                }
            }
            else
            {// Single subscribe to all.
                DoUpdateSubscription(request.SenderId, request.DelegateInstanceMethodInfo, SubscribeToAllId, request.IsAdd, request.SpecificCountOptional);
            }

        }

        void DoUpdateSubscription(ClientId subscriberId, MethodInfo subscriberMethodInfo,
                                  ClientId targetSourceId, bool addSubscription, int? specificValue)
        {
            HotSwapDictionary<ClientId, ClientEventSubscriptionInfo> data;
            if (_subscriptionsHotSwap.TryGetValue(targetSourceId, out data) == false)
            {// Create a new list for this id.
                data = _subscriptionsHotSwap.GetOrAdd(targetSourceId, new HotSwapDictionary<ClientId, ClientEventSubscriptionInfo>());
            }

            ClientEventSubscriptionInfo dataInfo;
            if (data.TryGetValue(subscriberId, out dataInfo) == false)
            {
                dataInfo = data.GetOrAdd(subscriberId, new ClientEventSubscriptionInfo());
            }

            dataInfo.Update(subscriberMethodInfo, addSubscription, specificValue);
        }
    }
}
