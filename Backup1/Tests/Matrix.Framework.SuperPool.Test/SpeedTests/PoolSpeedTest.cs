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
    /// <summary>
    /// Test the speed of super pool calls.
    /// Current config gives around 240K in Debug and 480K in Release, with call context enabled.
    /// </summary>
    public class PoolSpeedTest : SpeedTest, Interface1
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

        public bool ContextDataEnabled
        {
            get
            {
                return SuperPoolInvocation.CallContextEnabled;
            }

            set
            {
                SuperPoolInvocation.CallContextEnabled = value;
            }
        }

        public event EventHandler Event1;

        /// <summary>
        /// Constructor.
        /// </summary>
        public PoolSpeedTest()
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
            if (_testEventHandling)
            {
                client1.SubscribeAll<Interface1>().Event1 += new EventHandler(SuperPoolSpeedTest_Event1);
                //client1.Subscribe<Interface1>().Event1 -= new EventHandler(SuperPoolSpeedTest_Event1);
            }

            SystemMonitor.Info("Start...");

            if (Event1 != null)
            {
                Event1(this, EventArgs.Empty);
            }

            client1.Call<Interface1>(client1.Id).Prop1 = 73;

            for (int i = 0; i < count; i++)
            {
                //AA x = pool.Call<Interface1>(client1.Id).Run(2);
                //string res = pool.Call<Interface1>(client1.Id).Run(string.Empty);
                client1.Call<Interface1>(client1.Id).Run3();
                //int xa = client2.Call<Interface1>().Prop1;

                //int result = client1.CallSync<Interface1>(client1.Id, TimeSpan.FromSeconds(2)).Run2();
                //if (result != i)
                //{
                //    int h = 11;
                //}
                //x = pool.Call<Interface1>(6).Run(2);
            }

            if (_testEventHandling)
            {
                client1.SubscribeAll<Interface1>().Event1 -= new EventHandler(SuperPoolSpeedTest_Event1);
            }

            //pool._builder.Save();
            return true;
        }

        void SuperPoolSpeedTest_Event1(object sender, EventArgs e)
        {
            int h = 2;
        }

        #region Interface1 Members

        public AA Run(int x)
        {
            throw new NotImplementedException();
        }

        public string Run(string a)
        {
            Interlocked.Increment(ref _executed);
            if (_executed >= Count)
            {
                SignalTestComplete();
            }
            return a;
        }

        public int Run2()
        {
            return IncrementExecuted() - 1;
        }

        public void Run3()
        {
            IncrementExecuted();
            //Interlocked.Increment(ref _executed);
            //if (_executed >= Count)
            //{
            //    SignalTestComplete();
            //}
        }

        #endregion


        #region Interface1 Members


        public event MyDelegate MyEvent;

        #endregion

        #region Interface1 Members

        int _prop1 = 0;
        public int Prop1
        {
            get
            {
                IncrementExecuted();
                return _prop1;
            }

            set
            {
                _prop1 = value;
            }
        }

        int _propGet = 0;
        public int PropGet
        {
            get
            {
                IncrementExecuted();
                return _propGet;
            }

            set
            {
                _propGet = value;
            }
        }

        int _propSet = 0;
        public int PropSet
        {
            set { _propSet = value; }
        }

        #endregion
    }

    public struct AA
    {
    }

    public delegate int MyDelegate(object state, double t);

    [SuperPoolInterface]
    public interface Interface1
    {
        int Prop1 { get; set; }
        int PropGet { get; set; }
        int PropSet { set; }

        AA Run(int x);
        string Run(string a);

        int Run2();
        void Run3();

        event EventHandler Event1;
        event MyDelegate MyEvent;
    }
}
