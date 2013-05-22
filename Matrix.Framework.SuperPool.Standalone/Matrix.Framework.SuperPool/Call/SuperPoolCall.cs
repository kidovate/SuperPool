// -----
// Copyright 2010 Deyan Timnev
// This file is part of the Matrix Platform (www.matrixplatform.com).
// The Matrix Platform is free software: you can redistribute it and/or modify it under the terms of the GNU Lesser General Public License as published by the Free Software Foundation, 
// either version 3 of the License, or (at your option) any later version. The Matrix Platform is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; 
// without even the implied warranty of  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Lesser General Public License for more details.
// You should have received a copy of the GNU Lesser General Public License along with the Matrix Platform. If not, see http://www.gnu.org/licenses/lgpl.html
// -----
using System;
using System.Reflection;
using System.Windows.Forms;
using Matrix.Common.Core;
using Matrix.Common.Extended.FastSerialization;
using Matrix.Framework.SuperPool.Core;
using System.Runtime.Serialization;
using Matrix.Common.Core.Serialization;

namespace Matrix.Framework.SuperPool.Call
{
    /// <summary>
    /// Defines the parameters of a call to a super pool item.
    /// </summary>
    [Serializable]
    public class SuperPoolCall : ISerializable, ICloneable
    {
        public enum StateEnum : int
        {
            Requesting,
            Responding,
            EventRaise,
            Finished,
        }

        /// <summary>
        /// This is the request call id, both when this is a call or a response.
        /// </summary>
        public long Id { get; protected set; }
        public StateEnum State = StateEnum.Requesting;

        public bool RequestResponse = false;
        public object[] Parameters = null;

        /// <summary>
        /// Used only for serializatio, sometimes we may not be able to 
        /// construct a full MethodInfo on a remote location, so to
        /// still preserve the data, we store it here.
        /// </summary>
        string _methodInfoName = null;

        MethodInfo _methodInfoLocal = null;

        public MethodInfo MethodInfoLocal
        {
            get { return _methodInfoLocal; }
            set { _methodInfoLocal = value; }
        }

        /// <summary>
        /// Helper delegate type, used when performing invokes on Control instances.
        /// </summary>
        public delegate object ControlInvokeDelegate(MethodInfo methodInfo, Control control, object[] parameters);
        
        /// <summary>
        /// In case an exception has occured in a proxy call and this is the response,
        /// the exception is provided here.
        /// </summary>
        public Exception Exception
        {
            get
            {
                object[] parameters = Parameters;
                if (State == StateEnum.Responding && parameters.Length > 1)
                {
                    return parameters[1] as Exception;
                }

                return null;
            }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public SuperPoolCall(long id)
        {
            Id = id;
        }

        #region ISerializable Members

        /// <summary>
        /// Implementing the ISerializable to provide a faster, more optimized
        /// serialization for the class.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        public SuperPoolCall(SerializationInfo info, StreamingContext context)
        {
            // Get from the info.
            SerializationReader reader = new SerializationReader((byte[])info.GetValue("data", typeof(byte[])));
         
            Id = reader.ReadInt64();
            State = (StateEnum)reader.ReadInt32();
            RequestResponse = reader.ReadBoolean();
            Parameters = reader.ReadObjectArray();
            string methodInfoName = reader.ReadString();
            
            _methodInfoName = methodInfoName;
            MethodInfoLocal = SerializationHelper.DeserializeMethodBaseFromString(_methodInfoName, true);
        }

        /// <summary>
        /// Implementing the ISerializable to provide a faster, more optimized
        /// serialization for the class.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            SerializationWriter writer = new SerializationWriter();

            writer.Write(Id);
            writer.Write((int)State);
            writer.Write(RequestResponse);
            writer.Write(Parameters);
            
            // It is possible to not be able to construct method info here;
            // in this case we shall only contain the name, since we may
            // need it later on.
            if (MethodInfoLocal != null)
            {
                writer.Write(SerializationHelper.SerializeMethodBaseToString(MethodInfoLocal, true));
            }
            else if (string.IsNullOrEmpty(_methodInfoName) == false)
            {
                writer.Write(_methodInfoName);
            }
            else
            {
                writer.Write(string.Empty);
            }

            // Put to the info.
            info.AddValue("data", writer.ToArray());
        }

        #endregion

        /// <summary>
        /// *SLOW* Perform the actual call here to the target control object.
        /// Control calls are more complex, since they get executed on the control's 
        /// Invoke() thread, and to do this, we need the actual delegate instance.
        /// </summary>
        protected object CallControlInvoke(Control target, out Exception exception)
        {
            if (Matrix.Framework.SuperPool.Core.SuperPool.CallContextEnabled)
            {
                SuperPoolCallContext.CurrentCall = this;
            }

            object result = null;
            exception = null;

            try
            {
                ControlInvokeDelegate delegateInstance = 
                    delegate(MethodInfo methodInfo, Control controlTarget, object[] parameters)
                {
                    return FastInvokeHelper.CachedInvoke(methodInfo, controlTarget, parameters);
                };

                // Synchronously perform the invocation.
                result = target.Invoke(delegateInstance, new object[] { MethodInfoLocal, target, Parameters });
                
            }
            catch (Exception ex)
            {
                exception = ex;
            }

            if (Matrix.Framework.SuperPool.Core.SuperPool.CallContextEnabled)
            {
                SuperPoolCallContext.CurrentCall = null;
            }

            return result;
        }

        /// <summary>
        /// Perform the actual call here to the target object.
        /// </summary>
        public object Call(object target, bool autoControlInvoke, out Exception exception)
        {
            if (autoControlInvoke && target is Control)
            {
                return CallControlInvoke((Control)target, out exception);
            }

            if (Matrix.Framework.SuperPool.Core.SuperPool.CallContextEnabled)
            {
                SuperPoolCallContext.CurrentCall = this;
            }

            exception = null;
            object result = null;
            try
            {
                // This call is very fast since it uses the static cache in the helper.
                result = FastInvokeHelper.CachedInvoke(MethodInfoLocal, target, Parameters);

                // This conventional invoke gives around 1 Million executions per second load by itself.
                // TODO: optimization can be done using the DelegateTypeCache from CallControlInvoke(), 
                // since it uses the actual strongly typed delegates already.
                //result = MethodInfo.Invoke(target, Parameters);
            }
            catch (Exception ex)
            {
                exception = ex;
            }

            if (Matrix.Framework.SuperPool.Core.SuperPool.CallContextEnabled)
            {
                SuperPoolCallContext.CurrentCall = null;
            }

            return result;
        }

        public SuperPoolCall Duplicate()
        {
            return new SuperPoolCall(this.Id) { State = this.State, MethodInfoLocal = this.MethodInfoLocal, 
                                                RequestResponse = this.RequestResponse, Parameters = this.Parameters };
        }

        #region ICloneable Members

        public object Clone()
        {
            return this.Duplicate();
        }

        #endregion
    }
}
