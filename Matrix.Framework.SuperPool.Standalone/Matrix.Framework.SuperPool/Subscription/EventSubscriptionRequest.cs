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
using System.Collections.ObjectModel;
using System.Reflection;

namespace Matrix.Framework.SuperPool.Subscription
{
    /// <summary>
    /// Instruct the way an event of a proxy must be interpreted.
    /// A request for perform subscription for given event.
    /// </summary>
    [Serializable]
    public class EventSubscriptionRequest
    {
        public ClientId SenderId { get; set; }

        public string ExtendedEventName { get; set; }

        /// <summary>
        /// Add or remove operation.
        /// </summary>
        public bool IsAdd { get; set; } 
        
        /// <summary>
        /// 
        /// </summary>
        //public MethodInfo EventAddMethodInfo { get; set; }
        
        /// <summary>
        /// The method that handles the event on the receiver object.
        /// </summary>
        public MethodInfo DelegateInstanceMethodInfo { get; set; }

        /// <summary>
        /// Assigned only when a remote synchronization is done.
        /// </summary>
        public int? SpecificCountOptional { get; set; }

        List<ClientId> _eventsSources = null;
        /// <summary>
        /// If the value is null, we consider it to be a subscription for all sources in general.
        /// </summary>
        public ReadOnlyCollection<ClientId> EventsSources
        {
            get
            {
                List<ClientId> eventsSources = _eventsSources;
                if (eventsSources == null)
                {
                    return null;
                }

                return eventsSources.AsReadOnly(); 
            }
        }

        /// <summary>
        /// Constructor, instruct registration for ANY source that raises the event.
        /// </summary>
        public EventSubscriptionRequest()
        {
        }

        /// <summary>
        /// Constructor, instruct registration for a specific source.
        /// </summary>
        public EventSubscriptionRequest(ClientId eventSourceId)
        {
            _eventsSources = new List<ClientId>();
            _eventsSources.Add(eventSourceId);
        }

        /// <summary>
        /// Constructor, instruct registration for a specific set of sources.
        /// </summary>
        public EventSubscriptionRequest(IEnumerable<ClientId> eventSourceId)
        {
            _eventsSources = new List<ClientId>(eventSourceId);
        }

        public override string ToString()
        {
            string sourcesMessage = _eventsSources != null ? _eventsSources.Count.ToString() : string.Empty;
            return base.ToString() + ", senderId[" + SenderId.ToString() + "], sources count [" +
                   sourcesMessage + "]";
        }
    }
}
