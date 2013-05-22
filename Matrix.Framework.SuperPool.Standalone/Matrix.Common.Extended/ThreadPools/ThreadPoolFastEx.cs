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
using System.Diagnostics;
using System.Reflection;
using Matrix.Common.Core;

namespace Matrix.Common.Extended.ThreadPools
{
    /// <summary>
    /// Class extends tha custom fast thread pool with a few easy to use features 
    /// (yet they are slow, so use with cautioun).
    /// </summary>
    public class ThreadPoolFastEx : ThreadPoolFast
    {
        static List<Type> OwnerTypes = new List<Type>(new Type[] { typeof(ThreadPoolFastEx) });

        //Dictionary<MethodInfo, FastInvokeHelper.FastInvokeHandlerDelegate> _methodDelegates = new Dictionary<MethodInfo, FastInvokeHelper.FastInvokeHandlerDelegate>();

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="name">Name of this thread pool</param>
        public ThreadPoolFastEx(string name)
            : base(name)
        {
        }

        /// <summary>
        /// Helper, obtain the correspoding fast delegate of a method info.
        /// </summary>
        public FastInvokeHelper.FastInvokeHandlerDelegate GetMessageHandler(MethodInfo methodInfo)
        {
            return FastInvokeHelper.GetMethodInvoker(methodInfo, true, true);
            //lock (_methodDelegates)
            //{
            //    if (_methodDelegates.ContainsKey(methodInfo))
            //    {
            //        return _methodDelegates[methodInfo];
            //    }

            //    FastInvokeHelper.FastInvokeHandlerDelegate resultHandler = FastInvokeHelper.GetMethodInvoker(methodInfo, true, true);
            //    _methodDelegates[methodInfo] = resultHandler;
            //    return resultHandler;
            //}
        }

        /// <summary>
        /// This is dreadfully slow and can overload CPU with only 3000 calls per second!
        /// </summary>
        /// <returns></returns>
        string ObtainCallerName()
        {
            if (Debugger.IsAttached)
            {
                MethodBase method = ReflectionHelper.GetExternalCallingMethod(2, OwnerTypes);
                if (method != null)
                {
                    return method.Name;
                }
            }

            return string.Empty;
        }

        /// <summary>
        /// Redefine, matches the .NET thread pool queue, for simple interchange.
        /// </summary>
        /// <param name="d"></param>
        /// <param name="args"></param>
        public void Queue(WaitCallback d, object param)
        {
            Queue((Delegate)d, new object[] { param });
        }

        /// <summary>
        /// Add a new delegate call to be executed.
        /// This call gives a performance of approx 300 - 400 K calls per second, so it is not very fast.
        /// For more speed, use the QueueFastDelegate and the QueueTargetInfo methods (1.5-3.5 Mil on a Dual Core).
        /// </summary>
        public void Queue(Delegate d, params object[] args)
        {
            if (d == null)
            {
                return;
            }

            // This is 3000 calls per seconds SLOW.
            string callerName = ObtainCallerName();

            // The "d.Method" call is AWFULLY SLOW.
            QueueTargetInfo(new TargetInfo(callerName, d.Target, GetMessageHandler(d.Method), false, this, args));
        }



    }
}
