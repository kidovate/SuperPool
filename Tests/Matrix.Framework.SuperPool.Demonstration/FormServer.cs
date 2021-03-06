// -----
// Copyright 2010 Deyan Timnev
// This file is part of the Matrix Platform (www.matrixplatform.com).
// The Matrix Platform is free software: you can redistribute it and/or modify it under the terms of the GNU Lesser General Public License as published by the Free Software Foundation, 
// either version 3 of the License, or (at your option) any later version. The Matrix Platform is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; 
// without even the implied warranty of  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Lesser General Public License for more details.
// You should have received a copy of the GNU Lesser General Public License along with the Matrix Platform. If not, see http://www.gnu.org/licenses/lgpl.html
// -----
using System;
using System.Windows.Forms;
using System.Diagnostics;
using System.Reflection;
using Matrix.Common.Core.Identification;
using Matrix.Common.Diagnostics;
using Matrix.Common.Diagnostics.TracerCore;
using Matrix.Common.Extended;
using Matrix.Framework.MessageBus.Net;
using Matrix.Framework.SuperPool.Clients;
using Matrix.Framework.SuperPool.Core;
using Matrix.Framework.MessageBus.Core;
using System.Threading;

namespace Matrix.Framework.SuperPool.Demonstration
{
    /// <summary>
    /// The main Server form. It also implements the [ICommunicationInterface] and
    /// uses a SuperPoolClient to participate as a end point in the Super Pool communication.
    /// </summary>
    public partial class FormServer : Form, ICommunicationInterface
    {
        Matrix.Framework.SuperPool.Core.SuperPool _pool;
        SuperPoolClient _poolClient;

        /// <summary>
        /// Constructor.
        /// </summary>
        public FormServer()
        {
            InitializeComponent();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            //// [Optional] Assign the default tracer, to provide system wide tracing functionality.
            //SystemMonitor.AssignTracer(new Tracer());
            //this.tracerControl1.Tracer = SystemMonitor.Tracer;

            // Create the underlying (server) message bus and put the pool on it.
            ServerMessageBus messageBus = new ServerMessageBus("Server", null, null);
            _pool = new Matrix.Framework.SuperPool.Core.SuperPool(messageBus);

            // Create the client that will server as a connection between this
            // class and the super pool and add the client to the pool.
            _poolClient = new SuperPoolClient("Server", this);
            _pool.AddClient(_poolClient);

            // Finally subscribe to the event of having a client added to the bus/pool.
            _pool.MessageBus.ClientAddedEvent += new MessageBus.Core.MessageBusClientUpdateDelegate(MessageBus_ClientAddedEvent);
            messageBus.ClientRemovedEvent += (bus, id, remove) =>
            {
                this.Invoke(new GeneralHelper.GenericDelegate<string>(Report),
                            "Client removed " + id.ToString());
            };
        }

        void MessageBus_ClientAddedEvent(MessageBus.Core.IMessageBus messageBus, ClientId clientId)
        {// Once a new client has been added, we want to subscribe to its event.

            // Do the report on an invoke thread, since this message bus system event is raised on a non UI thread.
            this.Invoke(new GeneralHelper.GenericDelegate<string>(Report), "Subscribing client " + clientId.ToString());

            // We could also do a full subscribe like this _poolClient.Subscribe<ICommunicationInterface>() 
            // but since this only works on local components (attached to this super pool instance) and not
            // ones that are remoted (TCP), we need to subscribe to each one separately.
            _poolClient.Subscribe<ICommunicationInterface>(clientId).EventOne += new HelperDelegate(FormServer_EventOne);
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

        void Report(string message)
        {
            textBoxReport.AppendText(message + Environment.NewLine);
        }

        /// <summary>
        /// Create a new client instance, by launching the current "exe" with client parameter(s).
        /// </summary>
        private void toolStripButtonCreateClient_Click(object sender, EventArgs e)
        {
            Process notePad = new Process();
            notePad.StartInfo.FileName = Assembly.GetEntryAssembly().Location;
            notePad.StartInfo.Arguments = "client " + toolStripTextBoxClientName.Text;
            notePad.Start();
        }

        #region ICommunicationInterface Members

        public event HelperDelegate EventOne;

        public string DoWork(string parameter1)
        {
            // It is safe to directly access the UI elements here; since we are inside a [Win.Forms.Control] class child,
            // the super pool will automatically execute the calls we receive on the UI thread. This default behaviour 
            // is controllable trough the MessageSuperPoolClient.AutoControlInvoke flag, and can be disabled.
            Report(string.Format("Doing work [{0}].", parameter1));

            return "Server did some work.";
        }

        #endregion

        private void toolStripButtonCall_Click(object sender, EventArgs e)
        {
            Report("Sending work to all clients...");
            _poolClient.CallAll<ICommunicationInterface>().DoWork(toolStripTextBoxWorkParameter.Text);
        }

        private void toolStripButtonRaiseEvent_Click(object sender, EventArgs e)
        {
            HelperDelegate delegateInstance = EventOne;
            if (delegateInstance != null)
            {
                delegateInstance("Raise param");
                Report("Event raised...");
            }
        }
    }
}
