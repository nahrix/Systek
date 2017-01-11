using System;
using System.Collections.Generic;
using System.Linq;

namespace Systek.Net
{
    /// <summary>
    /// Describes the kind of Message that is being sent.
    /// </summary>
    [Serializable]
    public enum MessageType
    {
        /// <summary>
        /// Contains data for a set of commands
        /// </summary>
        COMMAND = 1,

        /// <summary>
        /// Gracefully close the connection
        /// </summary>
        CLOSE = 2,

        /// <summary>
        /// Log a message
        /// </summary>
        LOG = 3,

        /// <summary>
        /// Report a failure to execute a peer's request
        /// </summary>
        FAIL = 4,

        /// <summary>
        /// Report a successful command execution.
        /// </summary>
        SUCCESS = 5,

        /// <summary>
        /// Acknowledges replies, indicating that no further reply is necessary.
        /// </summary>
        ACK = 6,

        /// <summary>
        /// Indicates the message has timed out.
        /// </summary>
        TIMEOUT = 7,

        /// <summary>
        /// Send an update of the current state of the machine
        /// </summary>
        UPDATE_BASIC = 8,

        /// <summary>
        /// Sends an update of the state of services on the machine
        /// </summary>
        UPDATE_SERVICES = 9,

        /// <summary>
        /// Run a powershell script from a file.  The file path is defined in a <see cref="Systek.Net.Command" />
        /// <see cref="Systek.Net.Command.Cmd" /> defines the file path.
        /// <see cref="Systek.Net.Command.Parameters" /> defines the parameters, as a single string.  ie: "-Path C:\temp -Recurse -Force"
        /// </summary>
        RUN_POWERSHELL_SCRIPT = 10,

        /// <summary>
        /// Run a SQL script from a file.  The file path is defined in a <see cref="Systek.Net.Command" />
        /// <see cref="Systek.Net.Command.Cmd" /> defines the file path.
        /// <see cref="Systek.Net.Command.Parameters" /> defines the parameters, as a single string, with parameters separated by a space.
        /// </summary>
        RUN_SQL_SCRIPT = 11
    };


    /// <summary>
    /// A Message contains either a low-level control code, or a high-level CommandSet.
    /// The Systek.Net library will attempt to handle any control code, while the
    /// server or agent that utilizes this library should handle the Commands in the CommandSet.
    /// </summary>
    [Serializable]
    public struct Message
    {
        /// <summary>
        /// The type of message, defined in Systek.Net.MessageType
        /// </summary>
        public MessageType Type;

        /// <summary>
        /// The Command to be sent, if any.
        /// </summary>
        public ICommandSet CmdSet;

        /// <summary>
        /// The ID of the CommandSet referred to in this message
        /// </summary>
        public int CmdSetId;

        /// <summary>
        /// Identifies this message as part of a synchronous communication with the connected peer.
        /// This is used, for example, when a handshake is in progress.
        /// </summary>
        public bool Synchronized;

        /// <summary>
        /// Uniquely identifies the synchronous communication, so that multiple synchronous communications
        /// can happen in parallel.
        /// </summary>
        public int SyncId;

        /// <summary>
        /// A human-readable message.  Used to describe a log, for example.
        /// </summary>
        public List<string> Msg;

        /// <summary>
        /// The type of log, if there is one, defined in tblType.
        /// </summary>
        public Type LogType;

        /// <summary>
        /// The area type, as defined in tblAreaType, if applicable.
        /// </summary>
        public AreaType AreaType;

        /// <summary>
        /// The current state of the machine.
        /// </summary>
        public UpdateData Update;

        /// <summary>
        /// Determines whether the specified <see cref="Systek.Net.Message" />, is equal to this instance.
        /// </summary>
        /// <param name="other">The <see cref="Systek.Net.Message" /> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="Systek.Net.Message" /> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object other)
        {
            // Type check
            if ((other == null) || (other.GetType() != typeof(Message)))
            {
                return false;
            }

            Message test = (Message)other;

            // Comparison of primitives
            if ((Type != test.Type) || (Synchronized != test.Synchronized) || (SyncId != test.SyncId)
                || (LogType != test.LogType) || (CmdSetId != test.CmdSetId) || (AreaType != test.AreaType))
            {
                return false;
            }

            // Comparison of objects
            if ((!CmdSet?.Equals(test.CmdSet) ?? (test.CmdSet != null))
                || (!Update?.Equals(test.Update) ?? (test.Update != null))
                || (Msg != null && test.Msg != null && !Enumerable.SequenceEqual(Msg, test.Msg)))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
