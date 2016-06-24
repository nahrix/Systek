using Microsoft.VisualStudio.TestTools.UnitTesting;
using Systek.Net;
using Systek.Utility;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Systek.UnitTests
{
    [TestClass]
    public class NetTests
    {
        private const string LocalIP = "192.168.1.177";
        private const int LocalPort = 65000;

        /// <summary>
        /// Basic IConnection functionality testing.  Builds a TCPClient connection
        /// and passes it into 2 Connection objects representing agent + server, then makes a Message
        /// passes it through Connection's Send function, and checks the other Connection to see if
        /// it was received on the other end, and equivalent to the original Message.
        /// </summary>
        [TestMethod]
        public void IConnectionBaseTest()
        {
            // Create the endpoint representing the server, and listen for a connection
            IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Parse(LocalIP), LocalPort);

            TcpListener serverListener = new TcpListener(IPAddress.Any, LocalPort);
            serverListener.Start();
            IAsyncResult ar = serverListener.BeginAcceptTcpClient(null, null);

            // Create the TcpClient representing the agent's endpoint
            TcpClient agent = new TcpClient();
            agent.Connect(remoteEndPoint);

            // Create the TcpClient representing the server's endpoint
            TcpClient server = serverListener.EndAcceptTcpClient(ar);

            // Build the IConnections representing agent/server
            IConnection agentConnection = new Connection(agent, _LogHandler, _ExecuteHandler);
            IConnection serverConnection = new Connection(server, _LogHandler, _ExecuteHandler);

            agentConnection.Initialize();
            serverConnection.Initialize();

            // Mock Message to be passed from agent to server
            ICommand command1 = new Command(1, 1, "test command");
            ICommand command2 = new Command(1, 2, "test command 2");
            ICommand command3 = new Command(1, 3, "1234");

            ICommandSet set = new CommandSet(1, 3);
            set.AddCommand(command1);
            set.AddCommand(command2);
            set.AddCommand(command3);

            Message msg = new Message(MessageType.COMMAND, set);

            // Send the message
            agentConnection.Send(msg);

            // Give the Connection objects time to communicate, since they run in their own thread
            Thread.Sleep(100);

            // Pull the queue of Messages (1 message)
            List<Message> messages = serverConnection.GetMessages();

            // The test is that the sent message and receive message have equivilent content
            Assert.IsTrue(msg.Equals(messages[0]));

            // Clean up threads
            agentConnection.Close();
            serverConnection.Close();
        }

        // Handles log events
        private void _LogHandler(object sender, LogEventArgs e)
        {
            
        }

        // Handles execution events
        private bool _ExecuteHandler(object sender, ExecuteEventArgs e)
        {
            return true;
        }

        /// <summary>
        /// Tests AgentConnector and ServerConnector functions.
        /// </summary>
        [TestMethod]
        public void AgentServerConnectorTest()
        {
            IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Parse(LocalIP), LocalPort);
        }
    }
}
