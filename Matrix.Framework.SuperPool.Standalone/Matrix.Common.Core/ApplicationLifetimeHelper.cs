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
using System.Reflection;
using Matrix.Common.Core;

namespace Matrix.Common.Core
{
    /// <summary>
    /// Helps with the control and events of lifetime events of the application.
    /// In console applications make sure to call "SetApplicationClosing" upon close.
    /// In ApplicationDomains mode - tested under nunit and handled.
    /// In Service mode - not tested.
    /// </summary>
    public static class ApplicationLifetimeHelper
    {
        static System.Threading.Mutex _applicationMutex = null;

        static volatile Stopwatch _applicationStopwatch = null;
        /// <summary>
        /// The stopwatch measures time since the application was started.
        /// </summary>
        public static long ApplicationStopwatchTicks
        {
            get
            {
                return _applicationStopwatch.ElapsedTicks;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public static long ApplicationStopwatchMilliseconds
        {
            get
            {
                return _applicationStopwatch.ElapsedMilliseconds;
            }
        }

        /// <summary>
        /// CompletionEvent raised when application closing. Preffered to use this instead of Application.ApplicationExit
        /// since a Windows Service implementation may require modifications.
        /// </summary>
        public static event CommonHelper.DefaultDelegate ApplicationClosingEvent;

        volatile static bool _applicationClosing = false;
        /// <summary>
        /// 
        /// </summary>
        public static bool ApplicationClosing
        {
            get
            {
                //Console.
                if (_applicationClosing && Environment.HasShutdownStarted)
                {// A shutdown has started.
                    _applicationClosing = false;
                }

                return _applicationClosing;
            }
        }

        /// <summary>
        /// Static constructor.
        /// </summary>
        static ApplicationLifetimeHelper()
        {
            _applicationStopwatch = new Stopwatch();
            _applicationStopwatch.Start();

            //System.Diagnostics.Process process = System.Diagnostics.Process.GetCurrentProcess();
            //process.Exited += new EventHandler(process_Exited);

            // This greatly helps when the module is loaded inside an AppDomain (like in nUnit).
            AppDomain.CurrentDomain.DomainUnload += new EventHandler(CurrentDomain_DomainUnload);

            // This is a soft connection to the Application class, and will operate only 
            // when it is available in the using application.
            Type application = Type.GetType("System.Windows.Forms.Application, System.Windows.Forms, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
            if (application != null)
            {
                EventInfo exitEventInfo = application.GetEvent("ApplicationExit");
                if (exitEventInfo != null)
                {
                    exitEventInfo.AddEventHandler(null, new EventHandler(Application_ApplicationExit));
                }
            }
        }

        static void CurrentDomain_DomainUnload(object sender, EventArgs e)
        {
            SetApplicationClosing();
        }

        /// <summary>
        /// Creates a static application mutex, that is usefull for 
        /// checking if the application is already running.
        /// </summary>
        /// <returns>Returns false this call fails (already called this before), or true if it is good.</returns>
        public static bool TryGetApplicationMutex(string mutexName, out bool createdNew)
        {
            createdNew = false;
            if (_applicationMutex != null)
            {
                return false;
            }

            _applicationMutex = new System.Threading.Mutex(true, mutexName, out createdNew);
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        public static void ReleaseApplicationMutex()
        {
            if (_applicationMutex != null)
            {
                _applicationMutex.Close();
                _applicationMutex = null;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public static void SetApplicationClosing()
        {
            if (_applicationClosing == false)
            {
                _applicationClosing = true;

                CommonHelper.DefaultDelegate delegateInstance = ApplicationClosingEvent;
                if (delegateInstance != null)
                {
                    delegateInstance();
                }
            }
        }

        static void Application_ApplicationExit(object sender, EventArgs e)
        {
            SetApplicationClosing();

            //// On closing the application, release the threadpool resources and threads to speed up closing.
            //// _threadPoolEx.Dispose();

            //_applicationClosing = true;

            //if (ApplicationClosingEvent != null)
            //{
            //    ApplicationClosingEvent();
            //}
        }

        //static void process_Exited(object sender, EventArgs e)
        //{
        //    int h = 2;
        //}
    }
}
