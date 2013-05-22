// -----
// Copyright 2010 Deyan Timnev
// This file is part of the Matrix Platform (www.matrixplatform.com).
// The Matrix Platform is free software: you can redistribute it and/or modify it under the terms of the GNU Lesser General Public License as published by the Free Software Foundation, 
// either version 3 of the License, or (at your option) any later version. The Matrix Platform is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; 
// without even the implied warranty of  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Lesser General Public License for more details.
// You should have received a copy of the GNU Lesser General Public License along with the Matrix Platform. If not, see http://www.gnu.org/licenses/lgpl.html
// -----
using System;
using System.Threading;
using System.Reflection;
using Matrix.Common.Core.Serialization;
using Matrix.Framework.TestFramework;
using Matrix.Framework.SuperPool.Call;

namespace Matrix.Framework.SuperPool.Test.SpeedTests
{
    /// <summary>
    /// Test measures the s[eed of binary clone serialization.
    /// Performance: Test gives around 17-18K at current config.
    /// Much higher speed is achieved when using Duplicate(), in the millions.
    /// </summary>
    class SuperPoolCallSerializationSpeedTest : SpeedTest
    {
        /// <summary>
        /// 
        /// </summary>
        public SuperPoolCallSerializationSpeedTest()
            : base(false)
        {
        }

        public override void Update(FormTesting form)
        {
        }

        void Initialize()
        {
        }

        public override bool OnRun(FormTesting form, int count)
        {
            SuperPoolCall call = new SuperPoolCall(1712);

            // Generate a typical call.
            call.MethodInfoLocal = (MethodInfo)MethodInfo.GetCurrentMethod();
            call.Parameters = new object[] { "asdwdas", 85923, EventArgs.Empty };
            call.RequestResponse = false;
            call.State = SuperPoolCall.StateEnum.Finished;

            object result;
            for (_executed = 0; _executed < count; _executed++)
            {
                result = SerializationHelper.BinaryClone(call);
            }

            return true;
        }

    }
}
