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
using System.Threading;
using Matrix.Common.Extended;
using Matrix.Framework.MessageBus.Clients.ExecutionStrategies;
using Matrix.Framework.MessageBus.Core;
using Matrix.Framework.SuperPool.Clients;
using Matrix.Framework.SuperPool.Core;

namespace Matrix.Framework.SuperPool.Demonstration
{
    /// <summary>
    /// This class contains code that demonstrates how to assign a custom execution strategy.
    /// </summary>
    public class CustomExecutionStrategyDemonstration
    {
        public class CustomExecutionStrategy : ExecutionStrategy
        {
            protected override void OnExecute(Envelope envelope)
            {
                // Process the incoming request, we will simply execute in on default thread pool.
                WaitCallback del = delegate(object state)
                {
                    base.Client.PerformExecution((Envelope)envelope);
                };

                ThreadPool.QueueUserWorkItem(del, envelope);
            }
        }

        /// <summary>
        /// Creates a new client, assigns it with a custom execution strategy and adds it to the super pool.
        /// </summary>
        /// <param name="superPool"></param>
        public void Demonstrate(Matrix.Framework.SuperPool.Core.SuperPool superPool)
        {
            SuperPoolClient client = new SuperPoolClient("Client", this);
            client.SetupExecutionStrategy(new CustomExecutionStrategy());

            superPool.AddClient(client);
        }
    }
}
