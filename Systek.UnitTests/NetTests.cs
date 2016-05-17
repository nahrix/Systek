using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Systek.Net;
using System.Net.Sockets;
using System.Net;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading;

namespace Systek.UnitTests
{
    [TestClass]
    public class NetTests
    {
        private const string localIP = "192.168.0.199";
        private const int localPort = 65000;

        /// <summary>
        /// Basic IConnection functionality testing.  Builds a TCPClient connection
        /// and passes it into 2 Connection objects representing agent + server, then makes a Message
        /// passes it through Connection's Send function, and checks the other Connection to see if
        /// it was received on the other end, and equivalent to the original Message.
        /// </summary>
        [TestMethod]
        public void ICollectionBaseTest()
        {
            // Create the endpoint representing the server, and listen for a connection
            IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Parse(localIP), localPort);

            TcpListener serverListener = new TcpListener(IPAddress.Any, localPort);
            serverListener.Start();
            IAsyncResult ar = serverListener.BeginAcceptTcpClient(null, null);

            // Create the TcpClient representing the agent's endpoint
            TcpClient agent = new TcpClient();
            agent.Connect(remoteEndPoint);

            // Create the TcpClient representing the server's endpoint
            TcpClient server = serverListener.EndAcceptTcpClient(ar);

            // Build the IConnections representing agent/server
            IConnection agentConnection = new Connection(agent);
            IConnection serverConnection = new Connection(server);

            agentConnection.Initialize();
            serverConnection.Initialize();

            // Mock Message to be passed from agent to server
            Message msg = new Message();
            msg.Type = MessageType.COMMAND;
            msg.Data = "test command";
            msg.Sequence = 0;
            msg.Parameters = new List<string> { "one", "two" };

            // Send the message
            agentConnection.Send(msg);

            // Give the Connection objects time to communicate, since they run in their own thread
            Thread.Sleep(100);

            // Pull the queue of Messages (1 message)
            List<Message> messages = serverConnection.GetMessages();

            // The test is that the sent message and receive message have equivilent content
            Assert.AreEqual(msg, messages[0]);

            // Clean up threads
            agentConnection.Close();
            serverConnection.Close();
        }

        /// <summary>
        /// Tests AgentConnector and ServerConnector functions.
        /// </summary>
        [TestMethod]
        public void AgentServerConnectorTest()
        {
            IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Parse(localIP), localPort);
        }
    }
}
