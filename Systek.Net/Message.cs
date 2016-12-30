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
        /// Send an update of the current state of the machine
        /// </summary>
        UPDATE_BASIC = 5
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
        /// 0: Unused / asynchronous message
        /// 1: Begin synchrounous message
        /// 2: Continue synchrounous message
        /// 3: End synchronous message
        /// 4: Local synchronous message has been processed
        /// </summary>
        public int Synchronized;

        /// <summary>
        /// Uniquely identifies the synchronous communication, so that multiple synchronous communications
        /// can happen in parallel.
        /// </summary>
        public int SyncId;

        /// <summary>
        /// A human-readable message.  Used to describe a log, for example.
        /// </summary>
        public string Msg;

        /// <summary>
        /// The type of log, if there is one, defined in tblType.
        /// </summary>
        public int LogType;

        /// <summary>
        /// The area type, as defined in tblAreaType, if applicable.
        /// </summary>
        public int AreaType;

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
            if ((Type != test.Type) || (Synchronized != test.Synchronized) || (SyncId != test.SyncId) || (Msg != test.Msg)
                || (LogType != test.LogType) || (CmdSetId != test.CmdSetId) || (AreaType != test.AreaType))
            {
                return false;
            }

            // Comparison of objects
            if ((!CmdSet?.Equals(test.CmdSet) ?? (test.CmdSet != null))
                || (!Update?.Equals(test.Update) ?? (test.Update != null)))
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
