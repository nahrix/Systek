using Microsoft.VisualStudio.TestTools.UnitTesting;
using Systek.Net;
using System;
using System.Configuration;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Systek.UnitTests
{
    [TestClass]
    public class NetTests
    {
        private static string LocalIP;
        private static int LocalPort;
        private static bool ExecuteSuccess;
        private static Message TestMsg;
        private static bool Finished;

        // Initialize the variables for testing
        [ClassInitialize]
        public static void Initialize(TestContext context)
        {
            // Connection details
            LocalIP = ConfigurationManager.AppSettings["ServerIP"];
            LocalPort = Int32.Parse(ConfigurationManager.AppSettings["Port"]);

            // Flag for determining whether command execution succeeded
            ExecuteSuccess = false;

            // Flag will be true when all execution is complete, and Asserts are ready to be evaluated
            Finished = false;

            // Mock Message to be passed from agent to server
            ICommand command1 = new Command(1, 1, "test command");
            ICommand command2 = new Command(1, 2, "test command 2");
            ICommand command3 = new Command(1, 3, "1234");

            ICommandSet set = new CommandSet(1);
            set.AddCommand(command1);
            set.AddCommand(command2);
            set.AddCommand(command3);

            TestMsg = new Message();
            TestMsg.Type = MessageType.COMMAND;
            TestMsg.CmdSet = set;
        }

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
            IConnection agentConnection = new Connection(agent, _LogHandler, _AgentMessageHandler);
            IConnection serverConnection = new Connection(server, _LogHandler, _ServerMessageHandler);

            agentConnection.Initialize();
            serverConnection.Initialize();

            // Send the message
            serverConnection.Send(TestMsg);

            // Wait for the message to send, since communication happens in its own thread
            while (!Finished)
            {
                Thread.Sleep(100);
            }

            Assert.IsTrue(agentConnection.Connected);
            Assert.IsTrue(serverConnection.Connected);

            // Clean up threads
            agentConnection.Close();
            serverConnection.Close();

            Assert.IsTrue(ExecuteSuccess);
        }

        // Handles log events
        private void _LogHandler(LogEventArgs e)
        {
            
        }

        // Handles execution events
        private void _AgentMessageHandler(Message msg)
        {
            ExecuteSuccess = msg.Equals(TestMsg);
            Finished = true;
        }

        // Handles execution events
        private void _ServerMessageHandler(Message msg)
        {

        }

        /// <summary>
        /// Tests AgentConnector and ServerConnector functions.
        /// </summary>
        [TestMethod]
        public void AgentServerConnectorTest()
        {
            Server.ServerService server = new Server.ServerService();
            Agent.AgentService agent = new Agent.AgentService();

            server.Initialize();
            agent.Initialize();

            Thread.Sleep(1000);

            Assert.IsTrue(Server.Connector.Instance.Running);
            Assert.IsTrue(Agent.Core.Instance.Running);

            agent.Stop();
            server.Stop();

            Thread.Sleep(10000);

            Assert.IsFalse(Server.Connector.Instance.Running);
            Assert.IsFalse(Agent.Core.Instance.Running);
        }
    }
}
