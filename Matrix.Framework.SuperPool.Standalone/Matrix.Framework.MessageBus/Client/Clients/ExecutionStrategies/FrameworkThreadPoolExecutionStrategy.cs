// -----
// Copyright 2010 Deyan Timnev
// This file is part of the Matrix Platform (www.matrixplatform.com).
// The Matrix Platform is free software: you can redistribute it and/or modify it under the terms of the GNU Lesser General Public License as published by the Free Software Foundation, 
// either version 3 of the License, or (at your option) any later version. The Matrix Platform is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; 
// without even the implied warranty of  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Lesser General Public License for more details.
// You should have received a copy of the GNU Lesser General Public License along with the Matrix Platform. If not, see http://www.gnu.org/licenses/lgpl.html
// -----
using System.Threading;
using Matrix.Framework.MessageBus.Core;

namespace Matrix.Framework.MessageBus.Clients.ExecutionStrategies
{
    /// <summary>
    /// Implementation of the execution strategy using the default .net framework thread pool.
    /// </summary>
    public class FrameworkThreadPoolExecutionStrategy : ExecutionStrategy
    {
        WaitCallback _waitCallback;

        /// <summary>
        /// Constructor.
        /// </summary>
        public FrameworkThreadPoolExecutionStrategy()
        {
            _waitCallback = new WaitCallback(WaitCallbackFunc);
        }

        public override void Dispose()
        {
            _waitCallback = null;
            base.Dispose();
        }

        /// <summary>
        /// Enlist item for execution.
        /// </summary>
        protected override void OnExecute(Envelope envelope)
        {
            WaitCallback waitCallback = _waitCallback;
            if (waitCallback != null)
            {
                ThreadPool.QueueUserWorkItem(waitCallback, envelope);
            }
        }
        
        /// <summary>
        /// Execute item.
        /// </summary>
        protected void WaitCallbackFunc(object parameter)
        {
            ActiveClient client = Client;
            if (client != null)
            {
                client.PerformExecution(parameter as Envelope);
            }
        }

    }
}
