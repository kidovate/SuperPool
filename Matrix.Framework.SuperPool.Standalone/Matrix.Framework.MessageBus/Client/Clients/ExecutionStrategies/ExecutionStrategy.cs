// -----
// Copyright 2010 Deyan Timnev
// This file is part of the Matrix Platform (www.matrixplatform.com).
// The Matrix Platform is free software: you can redistribute it and/or modify it under the terms of the GNU Lesser General Public License as published by the Free Software Foundation, 
// either version 3 of the License, or (at your option) any later version. The Matrix Platform is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; 
// without even the implied warranty of  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Lesser General Public License for more details.
// You should have received a copy of the GNU Lesser General Public License along with the Matrix Platform. If not, see http://www.gnu.org/licenses/lgpl.html
// -----
#if Matrix_Diagnostics
using Matrix.Common.Diagnostics;
#endif

using System;
using Matrix.Framework.MessageBus.Core;

namespace Matrix.Framework.MessageBus.Clients.ExecutionStrategies
{
    /// <summary>
    /// Base class for thread execution strategies, for the Message bus client framework.
    /// </summary>
    public abstract class ExecutionStrategy : IDisposable
    {
        protected object _syncRoot = new object();

        ActiveClient _client;
        /// <summary>
        /// Instance of the client stub, this strategy serves on.
        /// </summary>
        protected ActiveClient Client
        {
            get { return _client; }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public ExecutionStrategy()
        {
        }

        /// <summary>
        /// Initialize the strategy.
        /// </summary>
        public bool Initialize(ActiveClient client)
        {
            lock (_syncRoot)
            {
                if (_client != null)
                {
#if Matrix_Diagnostics
                    SystemMonitor.Warning("Client already assigned to execution strategy.");
#endif
                    return false;
                }

                //SystemMonitor.ThrowIf(client != null, "Client not assigned to execution strategy.");
                _client = client;
                return true;
            }
        }

        public virtual void Dispose()
        {
            lock (_syncRoot)
            {
                _client = null;
            }
        }

        /// <summary>
        /// Enlist item for execution.
        /// </summary>
        public virtual void Execute(Envelope envelope)
        {
            ActiveClient client = _client;

            if (envelope.ExecutionModel == Envelope.ExecutionModelEnum.Direct)
            {
                if (client != null)
                {
                    client.PerformExecution(envelope);
                }
            }
            else
            {
                OnExecute(envelope);
            }
        }

        /// <summary>
        /// Implementations handle this to do the actual execution.
        /// </summary>
        protected abstract void OnExecute(Envelope envelope);

    }
}
