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
using Matrix.Common.Core.Serialization;
using NUnit.Framework;

namespace Matrix.Framework.SuperPool.UnitTest
{


    /// <summary>
    /// Test fixture class, tests various types of super pool calls.
    /// Tests are conducted with 2 local clients on a super pool.
    /// </summary>
    [TestFixture]
    public class CallTest
    {
        /// <summary>
        /// The tests are executed on a bunch of implementors, configured
        /// differently to make sure all tests run on various configurations.
        /// </summary>
        List<CallTestImplementor> _implementors = new List<CallTestImplementor>();

        CallTestImplementor _referenceImplementor;
        CallTestImplementor _binaryImplementor;

        /// <summary>
        /// Constructor.
        /// </summary>
        public CallTest()
        {
            // Load the implementors.
            _referenceImplementor = CreateDefaultImplementor();
            _implementors.Add(_referenceImplementor);
            
            _binaryImplementor = CreateBinaryLocalImplementor();
            _implementors.Add(_binaryImplementor);
            //_implementors.Add(CreateJSonLocalImplementor());
        }

        /// <summary>
        /// Implementor works on local referencing model.
        /// </summary>
        CallTestImplementor CreateDefaultImplementor()
        {
            Matrix.Framework.SuperPool.Core.SuperPool pool = new Matrix.Framework.SuperPool.Core.SuperPool("DefaultImplementor.Pool");
            
            CallTestImplementor implementor = new CallTestImplementor();
            pool.AddClient(implementor.Client1);
            pool.AddClient(implementor.Client2);
            implementor.Disposables.Add(pool);

            return implementor;
        }

        /// <summary>
        /// Implementor works on local referencing model.
        /// </summary>
        CallTestImplementor CreateBinaryLocalImplementor()
        {
            Matrix.Framework.MessageBus.Core.MessageBus bus = new Matrix.Framework.MessageBus.Core.MessageBus("BinaryLocalImplementor.Pool", new BinarySerializer());
            Matrix.Framework.SuperPool.Core.SuperPool pool = new Matrix.Framework.SuperPool.Core.SuperPool(bus);
            
            CallTestImplementor implementor = new CallTestImplementor();
            pool.AddClient(implementor.Client1);
            pool.AddClient(implementor.Client2);
            implementor.Disposables.Add(pool);

            implementor.Client1.EnvelopeDuplicationMode = Matrix.Framework.MessageBus.Core.Envelope.DuplicationModeEnum.DuplicateBoth;
            implementor.Client1.EnvelopeMultiReceiverDuplicationMode = Matrix.Framework.MessageBus.Core.Envelope.DuplicationModeEnum.DuplicateBoth;

            implementor.Client2.EnvelopeDuplicationMode = Matrix.Framework.MessageBus.Core.Envelope.DuplicationModeEnum.DuplicateBoth;
            implementor.Client2.EnvelopeMultiReceiverDuplicationMode = Matrix.Framework.MessageBus.Core.Envelope.DuplicationModeEnum.DuplicateBoth;

            return implementor;
        }

        ///// <summary>
        ///// Implementor works on local referencing model.
        ///// </summary>
        //CallTestImplementor CreateJSonLocalImplementor()
        //{
        //    MessageBus.MessageBus bus = new MessageBus.MessageBus("JSonLocalImplementor.Pool", new JSonSerializer());
        //    MessageSuperPool pool = new MessageSuperPool(bus);

        //    CallTestImplementor implementor = new CallTestImplementor();
        //    pool.AddClient(implementor.Client1);
        //    pool.AddClient(implementor.Client2);
        //    implementor.Disposables.Add(pool);

        //    implementor.Client1.DefaultEnvelopeDuplicationMode = MessageBus.Envelope.DuplicationModeEnum.DuplicateBoth;
        //    implementor.Client2.DefaultEnvelopeDuplicationMode = MessageBus.Envelope.DuplicationModeEnum.DuplicateBoth;

        //    return implementor;
        //}

        [TestFixtureSetUp]
        public void Init()
        {
            foreach (CallTestImplementor implementor in _implementors)
            {
                implementor.Initialize();
            }
        }

        [TestFixtureTearDown]
        public void UnInit()
        {
            foreach (CallTestImplementor implementor in _implementors)
            {
                implementor.Uninit();
            }
        }

        [Test]
        public void SimpleCallTestReference([Values(10000, 100000)] int length)
        {
            _referenceImplementor.SimpleCallTest(length);
        }

        [Test]
        public void SimpleCallTestBinarySerialization([Values(10000, 100000)] int length)
        {
            _binaryImplementor.SimpleCallTest(length);
        }

        [Test]
        public void VariableCallTest([Values(100)] int length)
        {
            foreach (CallTestImplementor implementor in _implementors)
            {
                implementor.VariableCallTest(length);
            }
        }

        [Test]
        public void RefCallTest()
        {
            foreach (CallTestImplementor implementor in _implementors)
            {
                implementor.RefCallTest();
            }
        }

        [Test]
        public void OutCallTest()
        {
            foreach (CallTestImplementor implementor in _implementors)
            {
                implementor.OutCallTest();
            }
        }

        [Test]
        public void AsyncResultCallTest()
        {
            foreach (CallTestImplementor implementor in _implementors)
            {
                implementor.AsyncResultCallTest();
            }
        }

        //[Test]
        //public void LongTest_AsyncTimeoutResultCallTest()
        //{
        //    foreach (CallTestImplementor implementor in _implementors)
        //    {
        //        implementor.AsyncTimeoutResultCallTest();
        //    }
        //}

        [Test]
        public void AsyncTimeoutResultCallTestException()
        {
            foreach (CallTestImplementor implementor in _implementors)
            {
                implementor.AsyncTimeoutResultCallTestException();
            }
        }

        [Test]
        public void CallConfirmedTest()
        {
            foreach (CallTestImplementor implementor in _implementors)
            {
                implementor.ConfirmedCallTest();
            }
        }

        /// <summary>
        /// This test is only runed on default implementor.
        /// </summary>
        [Test]
        public void DirectCall([Values(10000, 100000, 1000000)] int length)
        {
            _referenceImplementor.DirectCallTest(length);
        }

        [Test]
        public void CallFirst()
        {
            foreach (CallTestImplementor implementor in _implementors)
            {
                implementor.CallFirst();
            }
        }

    }
}

