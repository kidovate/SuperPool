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

namespace Matrix.Framework.SuperPool.Call
{
    /// <summary>
    /// Present per thread info for the current super pool call on a per thread base.
    /// Operation of this class is configurable, and can be stopped alltogether to speed
    /// up execution (controlled trough MessageSuperPoolInvocation.ProvideCallContextData).
    /// </summary>
    public static class SuperPoolCallContext
    {
        static Dictionary<int, SuperPoolCall> _calls = new Dictionary<int, SuperPoolCall>();

        /// <summary>
        /// Obtain (or set) the current call for the current thread.
        /// </summary>
        public static SuperPoolCall CurrentCall
        {
            get
            {
                SuperPoolCall result;
                lock (_calls)
                {
                    if (_calls.TryGetValue(Thread.CurrentThread.ManagedThreadId, out result))
                    {
                        return result;
                    }
                }

                return null;
            }

            internal set 
            {
                lock (_calls)
                {
                    _calls[Thread.CurrentThread.ManagedThreadId] = value;
                }
            }
        }
    }
}
