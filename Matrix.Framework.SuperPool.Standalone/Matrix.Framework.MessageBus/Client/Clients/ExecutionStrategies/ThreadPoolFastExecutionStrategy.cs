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
using Matrix.Common.Core;
using Matrix.Common.Extended;
using Matrix.Common.Extended.ThreadPools;
using Matrix.Framework.MessageBus.Core;

namespace Matrix.Framework.MessageBus.Clients.ExecutionStrategies
{
    /// <summary>
    /// Execution strategy implemented using the custom fast thread pool implementation.
    /// </summary>
    public class ThreadPoolFastExecutionStrategy : ExecutionStrategy
    {
        bool _useCommonMessageBusPool;

        volatile ThreadPoolFast _pool;

        FastInvokeHelper.FastInvokeHandlerDelegate _performExecutionDelegate = null;

        public ThreadPoolFast ThreadPool
        {
            get
            {
                if (_pool == null)
                {
                    lock (this)
                    {
                        if (_pool == null)
                        {
                            if (_useCommonMessageBusPool)
                            {
                                // Refactor caution!
                                if (Client.MessageBus != null)
                                {
                                    _pool = Client.MessageBus.DefaultThreadPool;
                                }
                            }
                            else
                            {
                                _pool = new ThreadPoolFast(Client != null  && Client.Id != null ? Client.Id.Name + ".ThreadPoolFast" : "NA.2.ThreadPoolFast");
                            }
                        }
                    }
                }

                return _pool;
            }
        }

        /// <summary>
        /// This only applicable when a custom local thread pool used.
        /// </summary>
        public int MinimumThreadsCount
        {
            get
            {
                ThreadPoolFast pool = ThreadPool;
                if (pool != null)
                {
                    return pool.MinimumThreadsCount;
                }
                return 0;
            }

            set
            {
                if (_useCommonMessageBusPool == false)
                {
                    ThreadPoolFast pool = ThreadPool;
                    if (pool != null)
                    {
                        pool.MinimumThreadsCount = value;
                    }
                }
            }
        }

        /// <summary>
        /// This only applicable when a custom local thread pool used.
        /// </summary>
        public int MaximumThreadsCount
        {
            get 
            { 
                ThreadPoolFast pool = ThreadPool;
                if (pool != null)
                {
                    return pool.MaximumThreadsCount;
                }
                return 0;
            }
            
            set
            {
                if (_useCommonMessageBusPool == false)
                {
                    ThreadPoolFast pool = ThreadPool;
                    if (pool != null)
                    {
                        pool.MaximumThreadsCount = value;
                    }
                }
            }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public ThreadPoolFastExecutionStrategy(bool useCommonMessageBusPool)
        {
            _useCommonMessageBusPool = useCommonMessageBusPool;

            GeneralHelper.GenericDelegate<Envelope> delegateInstance = new GeneralHelper.GenericDelegate<Envelope>(PerformExecution);
            _performExecutionDelegate = FastInvokeHelper.GetMethodInvoker(delegateInstance.Method, true, false);
        }

        public override void Dispose()
        {
            lock (_syncRoot)
            {
                if (_pool != null)
                {
                    _pool.Dispose();
                }
                _pool = null;
            }

            base.Dispose();
        }

        /// <summary>
        /// Actually perform the exection.
        /// </summary>
        internal void PerformExecution(object envelope)
        {
            ActiveClient client = Client;
            if (client != null)
            {
                client.PerformExecution(envelope as Envelope);
            }
        }

        protected override void OnExecute(Envelope envelope)
        {
            ThreadPoolFast pool = ThreadPool;
            if (pool != null)
            {
                pool.QueueFastDelegate(this, _performExecutionDelegate, envelope);
                
                // Other invocation options.
                //ThreadPoolFastEx.TargetInfo targetInfo = new ThreadPoolFastEx.TargetInfo(string.Empty, this,
                //    _singleFastInvokeDelegate, threadPool, envelope);
                //threadPool.QueueTargetInfo(targetInfo);
            }
        }
    }
}
