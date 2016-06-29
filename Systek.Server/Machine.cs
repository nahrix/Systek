﻿using Systek.Net;
using Systek.Utility;
using System;
using System.Collections.Generic;
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
        /// Initializes a new instance of the <see cref="Machine"/> class.
        /// </summary>
        /// <param name="agent">The connection to the agent.</param>
        public Machine(TcpClient agent)
        {
            Authenticated = false;
            MachineName = null;
            NetConnection = new Connection(agent, LogHandler, MessageHandler);
        }

        /// <summary>
        /// Authenticates this instance.
        /// </summary>
        /// <returns></returns>
        public bool Authenticate()
        {
            Update();

            return true;
        }

        public void Initialize()
        {
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
            if (!NetConnection.Connected)
            {
                return;
            }

            
        }

        /// <summary>
        /// Handle log events passed in from the Systek.Net library.
        /// </summary>
        /// <param name="e">The <see cref="LogEventArgs"/> instance containing the event data.</param>
        public void LogHandler(LogEventArgs e)
        {

        }

        /// <summary>
        /// Handles the messages sent from the agent.
        /// </summary>
        /// <param name="msg">The message to process.</param>
        public void MessageHandler(Message msg)
        {
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
                            NetConnection.Close();
                            break;

                        // TODO:  Handle each FAIL case
                        case MessageType.FAIL:
                            break;

                        // The Agent or Server projects will handle the events generated by LOGs
                        case MessageType.LOG:
                            Logger.Instance.TblSystemLog(msg.LogType, msg.AreaType, MachineID, msg.Msg);
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
                        case MessageType.UPDATE:
                            break;

                        default:
                            break;
                    }
                }
                
            }
            catch (Exception e)
            {
                Logger.Instance.TblSystemLog(1, 3, MachineID, "Error while processing a message from the agent.\n\n" + e.Message);
                NetConnection.Close();
            }
        }
    }
}
