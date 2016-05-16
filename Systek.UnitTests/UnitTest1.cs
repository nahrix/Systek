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
    public class ConnectionTests
    {
        private const string localIP = "192.168.1.64";
        private const int localPort = 65000;
        [TestMethod]
        public void TestSendAndRecieveMessage()
        {
            IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Parse(localIP), localPort);

            TcpListener serverListener = new TcpListener(IPAddress.Any, localPort);
            serverListener.Start();
            IAsyncResult ar = serverListener.BeginAcceptTcpClient(null, null);

            TcpClient agent = new TcpClient();
            agent.Connect(remoteEndPoint);

            TcpClient server = serverListener.EndAcceptTcpClient(ar);

            IConnection agentConnection = new Connection(agent);
            IConnection serverConnection = new Connection(server);

            agentConnection.Initialize();
            serverConnection.Initialize();

            Message msg = new Message();
            msg.Type = MessageType.COMMAND;
            msg.Data = "test command";
            msg.Sequence = 0;
            msg.Parameters = new List<string> { "one", "two" };

            agentConnection.Send(msg);

            Thread.Sleep(100);

            List<Message> messages = serverConnection.GetMessages();

            Thread.Sleep(100);

            Assert.AreEqual(msg, messages[0]);

            agentConnection.Close();
            serverConnection.Close();
        }
    }
}
