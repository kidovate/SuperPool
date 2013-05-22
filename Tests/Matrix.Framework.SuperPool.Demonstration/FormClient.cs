// -----
// Copyright 2010 Deyan Timnev
// This file is part of the Matrix Platform (www.matrixplatform.com).
// The Matrix Platform is free software: you can redistribute it and/or modify it under the terms of the GNU Lesser General Public License as published by the Free Software Foundation, 
// either version 3 of the License, or (at your option) any later version. The Matrix Platform is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; 
// without even the implied warranty of  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Lesser General Public License for more details.
// You should have received a copy of the GNU Lesser General Public License along with the Matrix Platform. If not, see http://www.gnu.org/licenses/lgpl.html
// -----
using System;
using System.Net;
using System.Windows.Forms;
using Matrix.Common.Diagnostics;
using Matrix.Common.Diagnostics.TracerCore;
using Matrix.Common.Extended;
using Matrix.Framework.MessageBus.Clients.ExecutionStrategies;
using Matrix.Framework.MessageBus.Net;
using Matrix.Framework.SuperPool.Clients;
using Matrix.Framework.SuperPool.Core;

namespace Matrix.Framework.SuperPool.Demonstration
{
    public partial class FormClient : Form, ICommunicationInterface
    {
        public string ClientName { get; set; }

        Matrix.Framework.SuperPool.Core.SuperPool _pool;
        SuperPoolClient _poolClient;

        /// <summary>
        /// Constructor.
        /// </summary>
        public FormClient()
        {
            InitializeComponent();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            this.Text += " - " + this.ClientName;

            // The steps here are exactly the same, as in server side, only difference is the ClientMessageBus 
            // instead of ServerMessageBus. Since this is the only difference, all the remaining source code
            // is completely independent of whether its a server or a client side.

            //// Assign the default tracer, to provide system wide tracing functionality.
            //SystemMonitor.AssignTracer(new Tracer());
            //this.tracerControl1.Tracer = SystemMonitor.Tracer;

            IPEndPoint endPoint = new IPEndPoint(IPAddress.Loopback, ServerMessageBus.DefaultPort);

            // Create the underlying (client) message bus, that takes care of transporting the 
            // actual communication messages; the message bus TCP communication is created
            // at default port, at localhost.
            ClientMessageBus messageBus = new ClientMessageBus(endPoint, this.ClientName, null);

            // Initialize the super pool with this message bus.
            _pool = new Matrix.Framework.SuperPool.Core.SuperPool(messageBus);

            // Create the client that will server as a connection between this
            // class and the super pool and add the client to the pool.
            _poolClient = new SuperPoolClient("Client." + this.ClientName, this);
            _pool.AddClient(_poolClient);

            messageBus.ClientAddedEvent += (bus, id) =>
                                               {
                                                   this.Invoke(new GeneralHelper.GenericDelegate<string>(Report),
                                                               "Client added " + id.ToString());
                                                   _poolClient.Subscribe<ICommunicationInterface>(id).EventOne += new HelperDelegate(FormServer_EventOne);
                                               };
            messageBus.ClientRemovedEvent += (bus, id, remove) =>
                                                 {
                                                     this.Invoke(new GeneralHelper.GenericDelegate<string>(Report),
                                                                 "Client removed " + id.ToString());
                                                 };
            // Use this to assign a specific execution strategy to a given client.
            // _poolClient.SetupExecutionStrategy(new FrameworkThreadPoolExecutionStrategy());
        }
        /// <summary>
        /// Process an event anonymously raised from a member of the pool.
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        object FormServer_EventOne(string param)
        {
            if (this.InvokeRequired)
            {
                throw new Exception("Wrong thread was applied by the super pool.");
            }

            // It is safe to directly access the UI elements here... see full description in DoWork method.
            Report("Received event raised..." + param);

            return "Accepted";
        }

        /// <summary>
        /// Helper method.
        /// </summary>
        /// <param name="message"></param>
        void Report(string message)
        {
            textBoxReport.AppendText(message + Environment.NewLine);
        }

        #region ICommunicationInterface Members

        public event HelperDelegate EventOne;

        public string DoWork(string parameter1)
        {
            // It is safe to directly access the UI elements here; since we are inside a [Win.Forms.Control] class child,
            // the super pool will automatically execute the calls we receive on the UI thread. This default behaviour 
            // is controllable trough the MessageSuperPoolClient.AutoControlInvoke flag, and can be disabled.
            Report("Doing work [" + parameter1 + "].");
            return string.Format("Client [{0}] did some work.", this.ClientName);
        }

        #endregion

        private void toolStripButtonRaiseEvent_Click(object sender, EventArgs e)
        {
            HelperDelegate del = EventOne;
            if (del != null)
            {
                string param = "client: " + _poolClient.Id.ToString() + ", param: " + this.toolStripTextBox1.Text;
                del(param);
                Report("Raised [" + param + "].");
            }
        }

        private void toolStripButtonCall_Click(object sender, EventArgs e)
        {
            Report("Sending work to server ...");
            
            // This will send a shout call to all those visible on the current super pool, that implement the interface.
            // Since we are single client on the local super pool, and connected to the server super pool, the call
            // will be sent to the server.
            _poolClient.CallAll<ICommunicationInterface>().DoWork(string.Format("Client[{0}] sends work [{1}].", this.ClientName, toolStripTextBox1.Text));
        }

        private void toolStripButtonDump_Click(object sender, EventArgs e)
        {
            Report("Dumping clients...");
            _poolClient.ResolveAll<ICommunicationInterface>().ForEach(n => Report(n.Name));
        }
    }
}
