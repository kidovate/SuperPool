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
using System.Diagnostics;

namespace Matrix.Common.Extended.ThreadPools
{
    /// <summary>
    /// This is a version of the thread pool using the .NET framework thread pool. It performs overall poorly.
    /// </summary>
    public class ThreadPoolEx2
    {
        /// <summary>
        /// 
        /// </summary>
        public ThreadPoolEx2()
        {
            System.Threading.ThreadPool.SetMaxThreads(55, 125);
        }

        public void Queue(Delegate d, params object[] args)
        {
            System.Threading.ThreadPool.QueueUserWorkItem(WaitCallbackInstance, new object[] { d, args });
        }

        void WaitCallbackInstance(object state)
        {
            object[] parameters = (object[])state;
            Delegate d = (Delegate)parameters[0];
            object[] callParams = (object[])parameters[1];

            d.DynamicInvoke(callParams);
        }

    }
}
