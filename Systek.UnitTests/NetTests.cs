using Microsoft.VisualStudio.TestTools.UnitTesting;
using Systek.Net;
using System;
using System.Collections.Generic;
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


        /// <summary>
        /// Initializes the specified context.
        /// </summary>
        /// <param name="context">The context containing test-specific data.</param>
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

            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add("badflag1", null);

            // Mock Message to be passed from agent to server
            ICommand command1 = new Command(CommandType.POWERSHELL, 1, "Get-ChildItems c:\\");
            ICommand command2 = new Command(CommandType.CONSOLE, 2, "ipconfig");
            ICommand command3 = new Command(CommandType.CONSOLE, 3, "ipconfig", parameters);

            ICommandSet set = new CommandSet(1, 3);
            set.AddCommand(command1);
            set.AddCommand(command2);
            set.AddCommand(command3);

            Dictionary<string, int> services = new Dictionary<string, int>();
            services.Add("test1", 1);
            services.Add("another", 2);
            services.Add("boing", 3);

            UpdateData data = new UpdateData();
            data.Services = services;

            TestMsg = new Message();
            TestMsg.Type = MessageType.COMMAND;
            TestMsg.CmdSet = set;
            TestMsg.Update = data;
            TestMsg.CmdSetId = 1;
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

            Agent.Core.Instance.NetConnection.Send(TestMsg);

            Thread.Sleep(1000);

            Server.IMachine agentMachine = Server.ConnectionManager.GetInstance().GetMachine(3);
            agentMachine.NetConnection.Send(TestMsg);

            Thread.Sleep(10000);

            agent.Stop();
            server.Stop();

            Thread.Sleep(3000);

            Assert.IsFalse(Server.Connector.Instance.Running);
            Assert.IsFalse(Agent.Core.Instance.Running);
        }
    }
}
