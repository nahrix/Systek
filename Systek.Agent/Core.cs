﻿using Systek.Net;
using Systek.Utility;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Management.Automation;
using System.Net;
using System.Net.Sockets;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Systek.Agent
{
    /// <summary>
    /// The core agent service.  Maintains a TCP connection, and handles messages from the server.
    /// </summary>
    public class Core
    {
        /// <summary>
        /// Gets the IConnection that handles low-level communications to the server.
        /// </summary>
        public IConnection NetConnection { get; private set; }

        /// <summary>
        /// Gets the TCP socket to the server.
        /// </summary>
        public TcpClient Server { get; private set; }

        /// <summary>
        /// Gets a value indicating whether this <see cref="Core"/> is running.
        /// </summary>
        /// <value>
        ///   <c>true</c> if running; otherwise, <c>false</c>.
        /// </value>
        public bool Running { get; private set; }

        /// <summary>
        /// Gets the path to the log files.
        /// </summary>
        public string LogPath { get; private set; }


        /// <summary>
        /// Gets the period of time between reconnect checks.
        /// </summary>
        private int _ReconnectWait;

        /// <summary>
        /// Used for writing logs in this class.
        /// </summary>
        private Logger _Log;

        /// <summary>
        /// Indicates whether logs should be verbose or not
        /// </summary>
        private bool _VerboseLogging = false;

        /// <summary>
        /// Gets or sets the singleton instance.
        /// </summary>
        private static Core _Instance;


        /// <summary>
        /// Describes the localhost ID when logging, as defined in tblServer
        /// </summary>
        private const int LOCALHOST = 1;


        /// <summary>
        /// Initializes a new instance of the <see cref="Core"/> class.  Privatized because this class is a singleton.
        /// </summary>
        private Core()
        {
            Running = false;
        }

        /// <summary>
        /// Returns the singleton instance of this class.
        /// </summary>
        /// <returns>This instance.</returns>
        public static Core Instance
        {
            get
            {
                if (_Instance == null)
                {
                    _Instance = new Core();
                }

                return _Instance;
            }
        }

        /// <summary>
        /// Initializes the connection to the specified remote end point.
        /// </summary>
        /// <param name="remoteEndPoint">The remote end point.</param>
        public bool Initialize(IPEndPoint remoteEndPoint)
        {
            try
            {
                bool parseResults = true;
                parseResults = parseResults && bool.TryParse(ConfigurationManager.AppSettings["VerboseLogging"], out _VerboseLogging);
                parseResults = parseResults && int.TryParse(ConfigurationManager.AppSettings["ReconnectWait"], out _ReconnectWait);
                string logPath = ConfigurationManager.AppSettings["LocalLogPath"];
                parseResults = parseResults && (logPath != null);

                // Initialization fails if any of the AppSettings values failed to load
                if (!parseResults)
                {
                    return false;
                }

                _Log = new Logger("AgentLogContext", logPath, "AgentCore");

                Running = true;
                Thread connector = new Thread(() => _Connector(remoteEndPoint));
                connector.Start();

                return true;
            }
            catch (Exception e)
            {
                Server?.Close();

                string message = "There was an exception thrown when trying to initialize the Agent:\n"
                    + e.Message + "\n\n" + e.StackTrace;
                _Log?.TblSystemLog(Type.ERROR, AreaType.AGENT_INITIALIZATION, LOCALHOST, message);

                return false;
            }
        }

        /// <summary>
        /// Stops the connection.
        /// </summary>
        public void Shutdown()
        {
            Running = false;
            _Log?.TblSystemLog(Type.INFO, AreaType.AGENT_INITIALIZATION, LOCALHOST, "Agent shutdown requested.");
            NetConnection?.Close();
            NetConnection = null;
        }

        /// <summary>
        /// To be run as its own thread.  Monitors the connection, and rebuilds it if it goes down.
        /// </summary>
        private void _Connector(IPEndPoint remoteEndPoint)
        {
            // This thread should run until the class' Shutdown function is called.
            do
            {
                DateTime reconnectLogTimer = DateTime.Now;

                try
                {
                    // Rebuild the connection if it's down
                    if (!NetConnection?.Connected ?? true)
                    {
                        _Log?.TblSystemLog(Type.INFO, AreaType.AGENT_INITIALIZATION, LOCALHOST, "Agent service is attempting to connect " +
                            " to server, at: IP " + remoteEndPoint.Address.ToString() + ", Port " + remoteEndPoint.Port.ToString());
                        Server = new TcpClient();
                        Server.Connect(remoteEndPoint);
                        NetConnection = new Connection(Server, _LogHandler, _MessageHandler);
                        NetConnection.VerboseLogging = _VerboseLogging;
                        NetConnection.Initialize();
                        Running = true;
                        _Log?.TblSystemLog(Type.INFO, AreaType.AGENT_INITIALIZATION, LOCALHOST, "Agent service connected to server successfully.");
                    }

                    // Wait before the next check, to minimize CPU usage
                    Thread.Sleep(_ReconnectWait);
                }
                catch (Exception e)
                {
                    if (DateTime.Now > reconnectLogTimer)
                    {
                        string message = "There was an exception thrown when trying to connect the Agent to the Server:\n" + e.Message
                            + "\n\n" + e.StackTrace;
                        _Log?.TblSystemLog(Type.ERROR, AreaType.AGENT_INITIALIZATION, LOCALHOST, message);

                        reconnectLogTimer = DateTime.Now.AddMilliseconds(_ReconnectWait * 100);
                    }

                    // Longer timeout to retry if the server appears to be down, to avoid log spam
                    Thread.Sleep(_ReconnectWait);
                }
            } while (Running);
        }

        /// <summary>
        /// Handles logging events passed from the Net library.
        /// </summary>
        /// <param name="e">The <see cref="LogEventArgs"/> instance containing the event data.</param>
        private void _LogHandler(LogEventArgs e)
        {
            // Prevents an attempt at logging while agent is shutting down, and the logging context
            // has been released from memory
            if (!Running)
            {
                return;
            }

            string message = "Agent log handler: " + e.Message;

            if (e.ExceptionDetail != null)
            {
                message += "\n" + e.ExceptionDetail.Message + "\n\n" + e.ExceptionDetail.StackTrace;
            }
            _Log?.TblSystemLog(e.Type, e.AreaType, LOCALHOST, message);
        }

        /// <summary>
        /// Handles messages sent from the server.
        /// </summary>
        /// <param name="msg">The MSG.</param>
        private void _MessageHandler(Message msg)
        {
            try
            {
                // Template reply.  Its values can be overwritten in custom scenarios.
                Message reply = new Message();
                reply.Synchronized = msg.Synchronized;
                reply.SyncId = msg.SyncId;
                reply.Type = MessageType.ACK;

                if (_VerboseLogging)
                {
                    _Log?.TblSystemLog(Type.INFO, AreaType.SERVER_MACHINE, LOCALHOST, "Agent is handling a message from server, " +
                        "of type: " + msg.Type.ToString());

                    Console.WriteLine("Message from server of type: " + Enum.GetName(msg.Type.GetType(), msg.Type));
                }

                switch (msg.Type)
                {
                    // NOTE: Maybe handle asynchronous ACKs in some way in the future
                    case MessageType.ACK:
                        return;

                    // A message with type UPDATE_BASIC is handled as a request, from the server, for this agent
                    // to provide some basic information about the client.  The data is sent back with the same
                    // UPDATE_BASIC message type.
                    case MessageType.UPDATE_BASIC:
                        reply.Type = MessageType.UPDATE_BASIC;
                        reply.Update = new UpdateData()
                        {
                            HostName = Environment.MachineName,
                            AuthKey = ConfigurationManager.AppSettings["AuthKey"]
                        };
                        break;

                    // Gather a list of all services, along with their current state, and send the list to the server
                    case MessageType.UPDATE_SERVICES:
                        Dictionary<string, int> services = new Dictionary<string, int>();

                        foreach (ServiceController service in ServiceController.GetServices())
                        {
                            services.Add(service.ServiceName, (int)service.Status);
                        }

                        reply.Type = MessageType.UPDATE_SERVICES;
                        reply.Update = new UpdateData()
                        {
                            Services = services
                        };
                        break;

                    // Execute a set of commands, defined by CommandSet.  Single commands are treated as
                    // a CommandSet of size 1.
                    case MessageType.COMMAND:
                        reply.CmdSet = msg.CmdSet;
                        reply = _ExecuteCommands(msg, reply);
                        break;
                    
                    // Close the network connection down, and shut down the agent service.
                    case MessageType.CLOSE:
                        break;

                    // Handle situations where a request from this agent has failed to execute on the server
                    case MessageType.FAIL:
                        if (msg.Msg != null)
                        {
                            foreach (string failMessage in msg.Msg)
                            {
                                _Log?.TblSystemLog(Type.ERROR, AreaType.AGENT_MESSAGE_HANDLER, LOCALHOST, failMessage);
                            }
                        }
                        break;

                    // Handle log requests from the server.
                    case MessageType.LOG:
                        break;

                    default:
                        break;
                }

                // Reply with either the default ACK, or other custom response generated by the message processor above
                NetConnection.Send(reply);
            }
            catch (Exception e)
            {
                _Log?.TblSystemLog(Type.ERROR, AreaType.AGENT_MESSAGE_HANDLER, LOCALHOST, "Error while processing a message from the server.\n\n" + e.Message);
            }
        }

        /// <summary>
        /// Executes commands that were passed in from the server via a COMMAND message.
        /// </summary>
        /// <param name="msg">The original COMMAND message from the server.</param>
        /// <param name="reply">The reply, which will contain the results of the execution.</param>
        /// <returns></returns>
        private Message _ExecuteCommands(Message msg, Message reply)
        {
            if (msg.CmdSet == null || msg.CmdSetId == 0)
            {
                reply.Type = MessageType.FAIL;
                return reply;
            }

            int i = 0;
            foreach (ICommand cmd in msg.CmdSet)
            {
                switch (cmd.CmdType)
                {
                    // ---------------------------------------------------------------------------------------
                    // Process windows-console commands ------------------------------------------------------
                    // ---------------------------------------------------------------------------------------
                    case CommandType.CONSOLE:
                        // Start the child process.
                        Process p = new Process();

                        // Redirect the output stream of the child process.
                        p.StartInfo.UseShellExecute = false;
                        p.StartInfo.RedirectStandardOutput = true;
                        p.StartInfo.FileName = cmd.Cmd;

                        if (cmd.Parameters != null)
                        {
                            string paramString = "";
                            foreach (KeyValuePair<string, string> parameter in cmd.Parameters)
                            {
                                paramString += "/" + parameter.Key + " " + parameter.Value;
                            }

                            p.StartInfo.Arguments = paramString;
                        }

                        string consoleOutput;

                        // Try to execute the command, and record its success or failure
                        try
                        {
                            p.Start();
                            consoleOutput = p.StandardOutput.ReadToEnd();
                            p.WaitForExit();

                            if (reply.Type != MessageType.FAIL)
                            {
                                reply.Type = MessageType.SUCCESS;
                            }
                            reply.CmdSet.Commands[i].Status = CommandStatus.SUCCCESS;
                        }
                        catch (Exception e)
                        {
                            reply.Type = MessageType.FAIL;
                            reply.CmdSet.Commands[i].Status = CommandStatus.FAIL;
                            consoleOutput = e.Message;
                        }

                        reply.CmdSet.Commands[i].Output.Add(consoleOutput);
                        break;

                    // ---------------------------------------------------------------------------------------
                    // Process Powershell commands -----------------------------------------------------------
                    // ---------------------------------------------------------------------------------------
                    case CommandType.POWERSHELL:
                        using (PowerShell powershellInstance = PowerShell.Create())
                        {
                            try
                            {
                                if (cmd.Parameters != null)
                                {
                                    powershellInstance.AddParameters(cmd.Parameters);
                                }

                                powershellInstance.AddScript(cmd.Cmd);

                                Collection<PSObject> PSOutput = powershellInstance.Invoke();

                                // Get the output of the successful execution
                                foreach (PSObject outputItem in PSOutput)
                                {
                                    if (outputItem != null)
                                    {
                                        reply.Msg.Add(outputItem.ToString());

                                        if (reply.Type != MessageType.FAIL)
                                        {
                                            reply.Type = MessageType.SUCCESS;
                                        }
                                        reply.CmdSet.Commands[i].Status = CommandStatus.SUCCCESS;
                                    }
                                }

                                // Get any error output
                                if (powershellInstance.Streams.Error.Count > 0)
                                {
                                    reply.Type = MessageType.FAIL;
                                    reply.CmdSet.Commands[i].Status = CommandStatus.FAIL;

                                    foreach (ErrorRecord error in powershellInstance.Streams.Error)
                                    {
                                        reply.CmdSet.Commands[i].Output.Add(error.ToString());
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                reply.Type = MessageType.FAIL;
                                reply.CmdSet.Commands[i].Status = CommandStatus.FAIL;
                                reply.CmdSet.Commands[i].Output.Add(e.Message);
                            }
                            
                        }

                        break;

                    // ---------------------------------------------------------------------------------------
                    // Process SQL commands ------------------------------------------------------------------
                    // ---------------------------------------------------------------------------------------
                    case CommandType.SQL:
                        break;
                }

                i++;
            }

            return reply;
        }
    }
}
