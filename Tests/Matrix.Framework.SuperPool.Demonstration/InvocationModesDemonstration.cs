// -----
// Copyright 2010 Deyan Timnev
// This file is part of the Matrix Platform (www.matrixplatform.com).
// The Matrix Platform is free software: you can redistribute it and/or modify it under the terms of the GNU Lesser General Public License as published by the Free Software Foundation, 
// either version 3 of the License, or (at your option) any later version. The Matrix Platform is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; 
// without even the implied warranty of  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Lesser General Public License for more details.
// You should have received a copy of the GNU Lesser General Public License along with the Matrix Platform. If not, see http://www.gnu.org/licenses/lgpl.html
// -----
using Matrix.Common.Core.Identification;
using Matrix.Framework.SuperPool.Clients;
using Matrix.Framework.SuperPool.Core;

namespace Matrix.Framework.SuperPool.Demonstration
{
    /// <summary>
    /// This is the interface that serves as a communication contract between 
    /// 2 or more components. There are requirements for it whatsoever, only to 
    /// have it market with the {SuperPoolInterfaceAttribute}.
    /// </summary>
    [SuperPoolInterface]
    public interface ISample
    {
        string MyMethod(int parameter1);
    }

    public class Component2 : ISample
    {
        public string MyMethod(int parameter1)
        {
            // Perform operation...
            // ...

            // Return result.
            return "Operation Result";
        }
    }

    /// <summary>
    /// This is the component class. It implements the communication interface,
    /// but can also inherit any class or interface. It can also implement multiple
    /// SuperPoolInterfaces and all of them will operate properly.
    /// </summary>
    public class Component : ISample
    {
        /// <summary>
        /// This is the pool client instance. It serves to connect the SuperPool
        /// mechanism with this component. It is not mandatory to have it inside 
        /// the component class, and may be used externally.
        /// </summary>
        public ISuperPoolClient client { get; protected set; }

        /// <summary>
        /// Contructor.
        /// </summary>
        public Component(string name)
        {
            client = new SuperPoolClient(name, this);
        }

        #region CommunicationInterface Members

        public string MyMethod(int parameter1)
        {
            return "Method result.";
        }

        #endregion

        /// <summary>
        /// This method shows a few ways to perform calls.
        /// </summary>
        public void PerformCalls(ISuperPoolClient otherClient)
        {
            ISample otherSource = otherClient.Source as ISample;
            string result;

            ComponentId recipientId = otherClient.Id;
            ComponentId[] recipientsIds = new ComponentId[] { otherClient.Id };

            AsyncCallResultDelegate asyncDelegate = delegate(ISuperPoolClient clientInstance, AsyncResultParams param)
                                                        {
                                                        };

            // Strongly Coupled Synchronous Invocation.
            // This is the typical, strong coupled way of communication, or invocation.
            result = otherSource.MyMethod(12);

            // Decoupled “DirectCall” Invocation (Very fast, Local only)
            // The closest invocation to the classical strongly coupled approach, this
            // method is very fast, synchronous, and loosely coupled.
            client.CallDirectLocal<ISample>(recipientId).MyMethod(12);

            // Decoupled Synchronous Invocation (Local and remote, Timeout configurable)
            client.CallSync<ISample>(recipientId).MyMethod(12);

            // Decoupled Asynchronous Invocation.
            client.Call<ISample>(recipientId).MyMethod(12);

            // Decoupled Asynchronous Invocation with Result.
            client.Call<ISample>(recipientId, asyncDelegate).MyMethod(12);

            // Decoupled Asynchronous Invocation to Multiple Receivers (Addressed or Non-addressed).
            client.Call<ISample>(recipientsIds).MyMethod(12); // Addressed
            client.CallAll<ISample>().MyMethod(12); // Non-addressed

        }

        void asyncResultMethod(ISuperPoolClient clientInstance, AsyncResultParams param)
        {
            // Handle async result.
        }
    }
}
