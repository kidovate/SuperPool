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
using Matrix.Common.Diagnostics;
using Matrix.Framework.TestFramework;
using Matrix.Framework.SuperPool.Clients;
using Matrix.Framework.SuperPool.Core;

namespace Matrix.Framework.SuperPool.Test.SpeedTests
{
    public interface DirectCallSpeedTest_Interface
    {
        int Run();
    }

    /// <summary>
    /// Test the speed of direct call.
    /// </summary>
    public class DirectCallSpeedTest : SpeedTest, DirectCallSpeedTest_Interface
    {
        Matrix.Framework.SuperPool.Core.SuperPool pool;
        SuperPoolClient client1;
        SuperPoolClient client2;

        bool _testEventHandling = true;
        /// <summary>
        /// 
        /// </summary>
        public bool TestEventHandling
        {
            get { return _testEventHandling; }
            set { _testEventHandling = value; }
        }

        public interface Interface
        {
            int Run2();
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public DirectCallSpeedTest()
            : base(true)
        {
            pool = new Matrix.Framework.SuperPool.Core.SuperPool();

            client1 = new SuperPoolClient("c1", this);
            client2 = new SuperPoolClient("c2", this);

            bool result = pool.AddClient(client1);
            result = pool.AddClient(client2);
        }

        public override void Update(FormTesting form)
        {
            
        }

        public override bool OnRun(FormTesting form, int count)
        {
            SystemMonitor.Info("Start...");

            for (int i = 0; i < count; i++)
            {
                client1.CallDirectLocal<DirectCallSpeedTest_Interface>(client2.Id).Run();
                //if (client1.DirectLocalCall<DirectCallSpeedTest_Interface>(client2.Id).Run() != 12)
                //{
                //    throw new Exception("Failure");
                //}
            }

            base.SignalTestComplete();
            return true;
        }

        public int Run()
        {
            base.IncrementExecuted();
            return 12;
        }
    }
}
