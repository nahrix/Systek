using Systek.Net;
using Systek.Utility;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Systek.Server
{
    /// <summary>
    /// A computer that is connected to the Systek server
    /// </summary>
    /// <seealso cref="Systek.Server.IMachine" />
    public class Machine : IMachine
    {
        /// <summary>
        /// Gets a value indicating whether this <see cref="IMachine" /> is authenticated.
        /// </summary>
        /// <value>
        ///   <c>true</c> if authenticated; otherwise, <c>false</c>.
        /// </value>
        public bool Authenticated { get; private set; }

        /// <summary>
        /// Gets a value indicating whether this <see cref="Machine"/> is disposed.
        /// </summary>
        /// <value>
        ///   <c>true</c> if disposed; otherwise, <c>false</c>.
        /// </value>
        public bool Disposed { get; private set; }

        /// <summary>
        /// Gets the name of the machine.
        /// </summary>
        public string MachineName { get; private set; }

        /// <summary>
        /// Gets the machine identifier.
        /// </summary>
        public int MachineID { get; private set; }

        /// <summary>
        /// Gets the Connection used to communicate with the
        /// </summary>
        public IConnection NetConnection { get; private set; }

        /// <summary>
        /// Used for writing logs in this class.
        /// </summary>
        private Logger Log { get; set; }

        /// <summary>
        /// Indicates whether verbose logs should be written.
        /// </summary>
        private bool VerboseLogging = false;

        /// <summary>
        /// The machine count, for debugging.
        /// </summary>
        private static int MachineCount = 0;

        /// <summary>
        /// Gets or sets the authentication key.
        /// </summary>
        private string AuthKey { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Machine" /> class.
        /// </summary>
        /// <param name="agent">The connection to the agent.</param>
        public Machine(TcpClient agent)
        {
            Boolean.TryParse(ConfigurationManager.AppSettings["VerboseLogging"], out VerboseLogging);
            Authenticated = false;
            Disposed = false;
            MachineName = null;
            MachineID = 3;
            MachineCount++;
            Log = new Logger("ServerLogContext", ConfigurationManager.AppSettings["localLogPath"], "AgentMachine");
            NetConnection = new Connection(agent, LogHandler, MessageHandler);
            NetConnection.VerboseLogging = VerboseLogging;

            if (VerboseLogging)
            {
                Log.TblSystemLog(Type.INFO, AreaType.SERVER_MACHINE, MachineID, "New Machine created.  MachineCount: "
                    + MachineCount.ToString());
            }
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="Machine"/> class.
        /// </summary>
        ~Machine()
        {
            Dispose(false);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposeManaged"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposeManaged)
        {
            if (Disposed)
            {
                return;
            }

            if (disposeManaged)
            {
                // Reserved for disposing any managed resources that this class might add
            }

            --MachineCount;

            if (VerboseLogging)
            {
                Log?.TblSystemLog(Type.INFO, AreaType.SERVER_MACHINE, MachineID, "Machine destroyed.  MachineCount: "
                    + MachineCount.ToString());
            }

            Log = null;

            NetConnection?.Dispose();
            NetConnection = null;

            Disposed = true;
        }

        /// <summary>
        /// Authenticates this instance.
        /// </summary>
        /// <returns></returns>
        public bool Authenticate()
        {
            if (VerboseLogging)
            {
                Log.TblSystemLog(Type.INFO, AreaType.SERVER_MACHINE, MachineID, "Machine is authenticating.");
            }

            Update();

            return true;
        }

        /// <summary>
        /// Initializes this instance.
        /// </summary>
        public void Initialize()
        {
            if (VerboseLogging)
            {
                Log.TblSystemLog(Type.INFO, AreaType.SERVER_MACHINE, MachineID, "Machine is initializing.");
            }

            NetConnection.Initialize();

            if (!NetConnection.Connected)
            {
                return;
            }

            Update();
        }

        /// <summary>
        /// Updates this instance with the latest state.
        /// </summary>
        public void Update()
        {
            if (VerboseLogging)
            {
                Log.TblSystemLog(Type.INFO, AreaType.SERVER_MACHINE, MachineID, "Machine is updating.");
            }

            if (!NetConnection.Connected)
            {
                return;
            }

            NetConnection.Send(new Message() { Type = MessageType.UPDATE_BASIC });
        }

        /// <summary>
        /// Handle log events passed in from the Systek.Net library.
        /// </summary>
        /// <param name="e">The <see cref="LogEventArgs"/> instance containing the event data.</param>
        public void LogHandler(LogEventArgs e)
        {
            string message = "Server log handler: " + e.Message;

            if (e.ExceptionDetail != null)
            {
                message += "\n" + e.ExceptionDetail.Message + "\n\n" + e.ExceptionDetail.StackTrace;
            }
            Log.TblSystemLog(e.Type, e.AreaType, MachineID, message);
        }

        /// <summary>
        /// Handles the messages sent from the agent.
        /// </summary>
        /// <param name="msg">The message to process.</param>
        public void MessageHandler(Message msg)
        {
            if (VerboseLogging)
            {
                Log.TblSystemLog(Type.INFO, AreaType.SERVER_MACHINE, MachineID, "Server is handling a message from machine "
                    + "with ID: " + MachineID + ".  Message type is: " + msg.Type.ToString());
            }

            try
            {
                // Process the full set of Messages if the agent is authenticated
                if (Authenticated)
                {
                    switch (msg.Type)
                    {
                        // Execute commands passed in by the agent
                        case MessageType.COMMAND:
                            break;

                        // Elegantly close the connection
                        case MessageType.CLOSE:
                            NetConnection.Dispose();
                            Dispose();
                            break;

                        // TODO:  Handle each FAIL case
                        case MessageType.FAIL:
                            break;

                        // The Agent or Server projects will handle the events generated by LOGs
                        case MessageType.LOG:
                            Log.TblSystemLog(msg.LogType, msg.AreaType, MachineID, msg.Msg);
                            break;

                        default:
                            break;
                    }
                }
                // Process only a select few messages if the agent is not authenticated
                else
                {
                    switch (msg.Type)
                    {
                        // Updates the state of the agent
                        case MessageType.UPDATE_BASIC:
                            MachineName = msg.Update.HostName;
                            AuthKey = msg.Update.AuthKey;
                            break;

                        // Elegantly close the connection
                        case MessageType.CLOSE:
                            NetConnection.Dispose();
                            Dispose();
                            break;

                        default:
                            break;
                    }
                }
                
            }
            catch (Exception e)
            {
                Log.TblSystemLog(Type.ERROR, AreaType.SERVER_MACHINE, MachineID, "Error while processing a message from the agent.\n\n" + e.Message);
                NetConnection.Dispose();
                Dispose();
            }
        }
    }
}
