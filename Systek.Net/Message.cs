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
        FAIL = 4
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
        /// The sequence number reference in this message
        /// </summary>
        public int Sequence;

        /// <summary>
        /// A human-readable message.  Used to describe a log, for example.
        /// </summary>
        public string Msg;

        /// <summary>
        /// The type of log, if there is one, defined in tblType.
        /// </summary>
        public int LogType;

        /// <summary>
        /// Constructor for COMMAND and CLOSE Message types
        /// </summary>
        /// <param name="type">The type of message, described in Systek.Net.MessageType</param>
        /// <param name="seq">The sequence number to start the CommandSet from.</param>
        /// <param name="cmds">The CommandSet to be run.</param>
        public Message(MessageType type, ICommandSet cmds = null, int seq = 1)
        {
            Type = type;
            Sequence = seq;
            CmdSet = cmds;
            Msg = null;
            LogType = 0;
            CmdSetId = cmds.ID;
        }

        /// <summary>
        /// Constructor for the LOG Message type
        /// </summary>
        /// <param name="msg">The log to be written at the peer.</param>
        /// <param name="type">The type of log, defined in tblType.</param>
        public Message(string msg, int type)
        {
            Type = MessageType.LOG;
            Sequence = 0;
            CmdSet = null;
            Msg = msg;
            LogType = type;
            CmdSetId = 0;
        }

        /// <summary>
        /// Constructor for the SUCCESS and FAIL Message types
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="cmdSetId">The command set identifier.</param>
        /// <param name="seq">The seq.</param>
        public Message(MessageType type, int cmdSetId, int seq)
        {
            Type = type;
            Sequence = seq;
            CmdSet = null;
            Msg = null;
            LogType = 0;
            CmdSetId = cmdSetId;
        }

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
            if ((Type != test.Type) || (Sequence != test.Sequence) || (Msg != test.Msg) || (LogType != test.LogType))
            {
                return false;
            }

            // Comparison of objects
            if (((CmdSet != null) && !CmdSet.Equals(test.CmdSet))
                || ((CmdSet == null) && (test.CmdSet != null)))
            {
                return false;
            }

            return true;
        }
    }
}
