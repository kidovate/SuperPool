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
using NUnit.Framework;
using Matrix.Framework.SuperPool.Core;
using Matrix.Framework.SuperPool.Clients;
using Matrix.Framework.MessageBus.Core;

namespace Matrix.Framework.SuperPool.UnitTest
{
    /// <summary>
    /// Helper delegate used for the testing of events.
    /// </summary>
    public delegate void HelperDelegate(int a, string b, EventArgs c);
    public delegate string HelperDelegateWithReturn(int a, string b, EventArgs c);

    /// <summary>
    /// Helper interface.
    /// </summary>
    [SuperPoolInterface]
    public interface ITestInterface
    {
        event HelperDelegate EventA;
        event HelperDelegateWithReturn EventB;

        /// <summary>
        /// Simple call.
        /// </summary>
        void SimpleMethod(int a, string b, EventArgs c);

        /// <summary>
        /// Async result call, will cause some delay to test sync/async result operation.
        /// </summary>
        string AsyncResultMethod(int requestedDelayMs);

        /// <summary>
        /// Variable parameters call.
        /// </summary>
        void VariableParametersTest(params object[] parameters);

        /// <summary>
        /// This "ref" parameter is not supported and must generate a NotImplementedException().
        /// </summary>
        void RefMethod(ref int a);

        /// <summary>
        /// This "out" parameter is not supported and must generate a NotImplementedException().
        /// </summary>
        void OutMethod(out int a);

        /// <summary>
        /// This method throws an exception.
        /// </summary>
        void ExceptionMethod();

        /// <summary>
        /// Handles a direct call, returns this.
        /// </summary>
        /// <returns></returns>
        ITestInterface DirectCall(string parameter1);
    }

    /// <summary>
    /// Implements the interface.
    /// </summary>
    public class InterfaceImplementor : ITestInterface
    {
        public delegate void StringDelegate(string value);

        public event StringDelegate MethodInvokedEvent;

        #region ITestInterface Members

        public event HelperDelegate EventA;

        public event HelperDelegateWithReturn EventB;

        public string Name { get; protected set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        public InterfaceImplementor(string name)
        {
            Name = name;
        }

        public void RaiseEventA(int a, string b, EventArgs c)
        {
            HelperDelegate delegateInstance = EventA;
            if (delegateInstance != null)
            {
                delegateInstance(a, b, c);
            }
        }

        public void RaiseEventB(int a, string b, EventArgs c)
        {
            HelperDelegateWithReturn delegateInstance = EventB;
            if (delegateInstance != null)
            {
                delegateInstance(a, b, c);
            }
        }

        public void VariableParametersTest(params object[] parameters)
        {
            // Method invoked.
            StringDelegate delegateInstance = MethodInvokedEvent;
            if (delegateInstance != null)
            {
                delegateInstance("VariableParametersTest");
            }
        }

        public void SimpleMethod(int a, string b, EventArgs c)
        {
            // Method invoked.
            StringDelegate delegateInstance = MethodInvokedEvent;
            if (delegateInstance != null)
            {
                delegateInstance("SimpleMethod");
            }
        }

        public void RefMethod(ref int a)
        {
            // Method invoked.
            StringDelegate delegateInstance = MethodInvokedEvent;
            if (delegateInstance != null)
            {
                delegateInstance("RefMethod");
            }
        }

        public string AsyncResultMethod(int requestedDelayMs)
        {
            Thread.Sleep(requestedDelayMs);
            return "async call completed";
        }

        public void OutMethod(out int a)
        {
            a = -1;
        }

        public void ExceptionMethod()
        {
            throw new NotImplementedException();
        }

        public ITestInterface DirectCall(string parameter1)
        {
            return this;
        }

        #endregion
    }

    /// <summary>
    /// Implements the operation of the Call test.
    /// </summary>
    internal class CallTestImplementor
    {
        internal SuperPoolClient Client1 { get; set; }
        internal SuperPoolClient Client2 { get; set; }

        InterfaceImplementor _implementor1;
        InterfaceImplementor _implementor2;

        /// <summary>
        /// List of items to be disposed at test end.
        /// </summary>
        internal List<IDisposable> Disposables = new List<IDisposable>();

        #region Default Parameters

        int a = 12;
        string b = "string b";
        EventArgs c = new EventArgs();

        #endregion


        /// <summary>
        /// Constructor.
        /// </summary>
        internal CallTestImplementor()
        {
            _implementor1 = new InterfaceImplementor("Implementor1");
            _implementor2 = new InterfaceImplementor("Implementor2");

            Client1 = new SuperPoolClient("client1", _implementor1);
            Client2 = new SuperPoolClient("client2", _implementor2);
        }

        internal void Initialize()
        {
        }

        internal void Uninit()
        {
            foreach (IDisposable disposable in Disposables)
            {
                disposable.Dispose();
            }

            // If we do this, following tests will fail.
            //ApplicationLifetimeHelper.SetApplicationClosing();
        }

        internal void SimpleCallTest(int length)
        {
            for (int i = 0; i < length; i++)
            {
                Client1.Call<ITestInterface>(Client2.Id).SimpleMethod(a, b, c);
            }
        }

        
        internal void VariableCallTest(int length)
        {
            for (int i = 0; i < length; i++)
            {
                Client1.Call<ITestInterface>(Client2.Id).VariableParametersTest(new object[] { a, b, c });
            }
        }

        
        internal void RefCallTest()
        {
            try
            {
                int d = 0;
                Client1.Call<ITestInterface>(Client2.Id).RefMethod(ref d);
                throw new Exception("Invalid call did not generate expected exception.");
            }
            catch (NotImplementedException)
            {
                // Test succeded only if exception is thrown.
            }
        }

        internal void ConfirmedCallTest()
        {
            CallOutcome outcome;
            Client1.CallConfirmed<ITestInterface>(Client2.Id, TimeSpan.FromSeconds(2), out outcome).SimpleMethod(2, "some param", EventArgs.Empty);

            if (outcome == null || outcome.Result != Outcomes.Success)
            {
                Assert.Fail("Failed to perform confirmed call.");
            }

            ClientId dummyId = new ClientId("dummyId");
            CallOutcome outcome2;
            Client1.CallConfirmed<ITestInterface>(dummyId, TimeSpan.FromSeconds(2), out outcome2).SimpleMethod(2, "some param", EventArgs.Empty);

            if (outcome2 != null && outcome2.Result == Outcomes.Success)
            {
                Assert.Fail("Failed to verify invalid outcome.");
            }
        }
        
        internal void OutCallTest()
        {
            try
            {
                int d = 0;
                Client1.Call<ITestInterface>(Client2.Id).OutMethod(out d);
                throw new Exception("Invalid call did not generate expected exception.");
            }
            catch (NotImplementedException)
            {
                // Test succeded only if exception is thrown.
            }
        }

        
        internal void AsyncResultCallTest()
        {
            ManualResetEvent eventa = new ManualResetEvent(false);

            AsyncCallResultDelegate delegateInstance =
                delegate(ISuperPoolClient client, AsyncResultParams parameters)
                {
                    if ((parameters.State is int == false) ||
                        (int)parameters.State != 152)
                    {
                        Assert.Fail("Parameter fail.");
                    }

                    if (parameters.Result != null)
                    {
                        // Do something with result.
                        string resultString = parameters.Result.ToString();
                    }

                    eventa.Set();
                };

            Client1.Call<ITestInterface>(Client2.Id, delegateInstance, 152).AsyncResultMethod(1500);

            if (eventa.WaitOne(60000) == false)
            {
                Assert.Fail("Failed to receive async result.");
            }
        }

        
        internal void AsyncTimeoutResultCallTest()
        {
            ManualResetEvent eventa = new ManualResetEvent(false);

            DateTime start = DateTime.Now;

            AsyncCallResultDelegate delegateInstance =
                delegate(ISuperPoolClient client, AsyncResultParams parameters)
                {
                    TimeSpan time = DateTime.Now - start;
                    Console.WriteLine("Async call result received in " + time.TotalMilliseconds + "ms.");
                    eventa.Set();
                    string p = parameters.Result as string;
                };

            Client1.CallAll<ITestInterface>(delegateInstance, 152, TimeSpan.FromSeconds(2)).AsyncResultMethod(500);

            eventa.WaitOne(60000);

            if (Client1.PendingSyncCallsCount != 1)
            {
                throw new Exception("Pending sync calls count not 1.");
            }

            // Allow time for the client GC to gather the call.
            Thread.Sleep(SuperPoolClient.GarbageCollectorIntervalMs * 3);

            if (Client1.PendingSyncCallsCount != 0)
            {
                throw new Exception("Pending sync calls count not 0.");
            }
        }

        internal void DirectCallTest(int length)
        {
            ITestInterface result = null;
            for (int i = 0; i < length; i++)
            {
                result = Client1.CallDirectLocal<ITestInterface>(Client2.Id).DirectCall("some data");   
            }
            
            Assert.AreEqual(result == Client2.Source, true, "Result failed.");
        }

        internal void CallFirst()
        {
            Client1.CallFirst<ITestInterface>().AsyncResultMethod(100);
        }

        internal void AsyncTimeoutResultCallTestException()
        {
            ManualResetEvent eventa = new ManualResetEvent(false);

            AsyncCallResultDelegate delegateInstance =
                delegate(ISuperPoolClient client, AsyncResultParams parameters)
                {
                    if (parameters.Exception == null)
                    {// No exception was received, this is not expected.
                    }
                    else
                    {
                        eventa.Set();
                    }
                };

            Client1.CallAll<ITestInterface>(delegateInstance, 152, TimeSpan.FromSeconds(2)).ExceptionMethod();

            if (eventa.WaitOne(20000) == false)
            {
                throw new Exception("Test failed due to no exception received.");
            }
        }
    }

}
