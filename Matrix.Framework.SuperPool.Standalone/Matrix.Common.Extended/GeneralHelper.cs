// -----
// Copyright 2010 Deyan Timnev
// This file is part of the Matrix Platform (www.matrixplatform.com).
// The Matrix Platform is free software: you can redistribute it and/or modify it under the terms of the GNU Lesser General Public License as published by the Free Software Foundation, 
// either version 3 of the License, or (at your option) any later version. The Matrix Platform is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; 
// without even the implied warranty of  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Lesser General Public License for more details.
// You should have received a copy of the GNU Lesser General Public License along with the Matrix Platform. If not, see http://www.gnu.org/licenses/lgpl.html
// -----
using System;
using System.Diagnostics;
using Matrix.Common.Core;
using Matrix.Common.Extended.ThreadPools;

namespace Matrix.Common.Extended
{
    /// <summary>
    /// A general helper class, contains all kinds of routines that help the general operation of an application.
    /// </summary>
    public class GeneralHelper : CommonHelper
    {
        /// <summary>
        /// Could use the default thread pool as well, but that does have a ~0.5sec delay when starting.
        /// </summary>
        static ThreadPoolFastEx _threadPoolEx = new ThreadPoolFastEx(typeof(GeneralHelper).Name);

        /// <summary>
        /// Explicit static constructor to tell C# compiler
        /// not to mark type as BeforeFieldInit. Required for 
        /// thread safety of static elements.
        /// </summary>
        static GeneralHelper()
        {
            _threadPoolEx.MaximumThreadsCount = 16;
            //_threadPoolEx.MaximumSimultaniouslyRunningThreadsAllowed = 12;
        }

        /// <summary>
        /// Helper.
        /// </summary>
        public static double TicksToMilliseconds(long ticks)
        {
            return ConvertTicksToMilliseconds(ticks);
        }

        /// <summary>
        /// Helper.
        /// </summary>
        public static double ConvertTicksToMilliseconds(long ticks)
        {
            return ((double)ticks / (double)Stopwatch.Frequency) * 1000;
        }


        #region Async Helper

        /// <summary>
        /// Redefine needed so that we can construct and pass anonymous delegates with parater.
        /// No need to specify the Value parameter(s), since they can be recognized automatically by compiler.
        /// </summary>
        public static void FireAndForget<Value>(GeneralHelper.GenericDelegate<Value> d, params object[] args)
        {
            _threadPoolEx.Queue(d, args);
        }

        /// <summary>
        /// Redefine needed so that we can construct and pass anonymous delegates with parater.
        /// No need to specify the Value parameter(s), since they can be recognized automatically by compiler.
        /// </summary>
        public static void FireAndForget<ValueType1, ValueType2>(GeneralHelper.GenericDelegate<ValueType1, ValueType2> d, params object[] args)
        {
            _threadPoolEx.Queue(d, args);
        }

        /// <summary>
        /// Redefine needed so that we can construct and pass anonymous delegates with parater.
        /// No need to specify the Value parameter(s), since they can be recognized automatically by compiler.
        /// </summary>
        public static void FireAndForget<ValueType1, ValueType2, ValueType3>(GenericDelegate<ValueType1, ValueType2, ValueType3> d, params object[] args)
        {
            _threadPoolEx.Queue(d, args);
        }

        /// <summary>
        /// Helper, fire and forget an execition on the delegate.
        /// </summary>
        public static void FireAndForget(Delegate d, params object[] args)
        {
            _threadPoolEx.Queue(d, args);
        }

        /// <summary>
        /// A fire and forget for a default delegate.
        /// </summary>
        /// <param name="theDelegate"></param>
        public static void FireAndForget(DefaultDelegate theDelegate)
        {
            FireAndForget(theDelegate, null);
        }

        #endregion

    }
}
