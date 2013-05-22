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
using Matrix.Framework.SuperPool.Core;
using Matrix.Framework.SuperPool.Clients;
using Matrix.Framework.MessageBus.Core;

namespace Matrix.Framework.SuperPool.Demonstration
{
    [SuperPoolInterface]
    public interface ISomeInterface
    {
        void ReceiveSomeInfo(string info);
        void DoSomeWork();
    }

    public class MyComponent : ISomeInterface
    {
        public SuperPoolClient Client { get; set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        public MyComponent()
        {
            Client = new SuperPoolClient("MyClient", this);
        }

        /// <summary>
        /// Send a request to "other" to do some work.
        /// </summary>
        public void RequestSomeWork(ClientId otherId)
        {
            Client.Call<ISomeInterface>(otherId).DoSomeWork();
        }

        #region ISomeInterface

        public void ReceiveSomeInfo(string info)
        {
            // ... someone send us some info.
        }

        public void DoSomeWork()
        {
            // ... doing work.
        }

        #endregion
    }

    public class MainClass
    {
        public void ShowMe()
        {
            // Create the pool.
            Matrix.Framework.SuperPool.Core.SuperPool pool = new Matrix.Framework.SuperPool.Core.SuperPool("MyPool");

            // Create component 1 and 2.
            MyComponent component1 = new MyComponent();
            MyComponent component2 = new MyComponent();

            // Add them both to the pool.
            pool.AddClient(component1.Client);
            pool.AddClient(component2.Client);

            // Request some work:
            component1.RequestSomeWork(component2.Client.Id);

            // Or we can also do it directly like this (although the previous is often a more suitable aproach)
            component1.Client.Call<ISomeInterface>(component2.Client.Id).DoSomeWork();
        }
    }
}
