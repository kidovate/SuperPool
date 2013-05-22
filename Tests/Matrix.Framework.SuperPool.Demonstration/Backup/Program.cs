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
using System.Windows.Forms;

namespace Matrix.Framework.SuperPool.Demonstration
{
    public class Program
    {
        static void RunServer()
        {
            FormServer server = new FormServer();
            Application.Run(server);
        }

        static void RunClient(string name)
        {
            FormClient client = new FormClient();
            client.ClientName = name;
            Application.Run(client);
        }

        /// <summary>
        /// Main application entry form.
        /// </summary>
        public static void Main(string[] args)
        {
            if (args.Length < 1)
            {// No parameters mean run as server.
                RunServer();
            }
            else
            {
                if (args[0] == "ask")
                {
                    if (MessageBox.Show("Run as server (yes for server, no for client)?", "Select Mode", MessageBoxButtons.YesNo) == DialogResult.Yes)
                    {
                        RunServer();
                    }
                    else
                    {
                        RunClient("MyClient");
                    }
                }

                if (args[0] == "client")
                {
                    string clientName = "MyClient";
                    if (args.Length > 1)
                    {
                        clientName= args[1];
                    }

                    RunClient(clientName);
                }
            }
        }
    }
}
