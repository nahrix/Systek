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
        /// Run a commandSet
        /// </summary>
        EXECUTE = 2,

        /// <summary>
        /// Gracefully close the connection
        /// </summary>
        CLOSE = 3,

        /// <summary>
        /// Delete a commandSet
        /// </summary>
        CLEAR = 4,

        /// <summary>
        /// Log a message
        /// </summary>
        LOG = 5,

        /// <summary>
        /// Report a failure to execute a peer's request
        /// </summary>
        FAIL = 6
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
        /// The sequence number in the command set to begin execution (default is 1)
        /// </summary>
        public int Sequence;

        /// <summary>
        /// Initializes a new instance of the <see cref="Message" /> struct.
        /// </summary>
        /// <param name="type">The type of message, described in Systek.Net.MessageType</param>
        /// <param name="seq">The sequence number to start the CommandSet from.</param>
        /// <param name="cmds">The CommandSet to be run.</param>
        public Message(MessageType type, ICommandSet cmds = null, int seq = 1)
        {
            Type = type;
            Sequence = seq;
            CmdSet = cmds;
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
            if ((other == null) || (other.GetType() != typeof(Message)))
            {
                return false;
            }

            Message test = (Message)other;
            bool test1 = (Type != test.Type);
            test1 = (Sequence != test.Sequence);
            test1 = CmdSet.Equals(test.CmdSet);

            if ((Type != test.Type) || (Sequence != test.Sequence) || (!CmdSet.Equals(test.CmdSet)))
            {
                return false;
            }

            return true;
        }
    }
}
