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
        EXECUTE = 1,        // Run any queued commands
        NEWSET = 2,         // Start building a new queue of commands
        COMMAND = 3,        // Queue a command
        CLOSE = 4,          // Gracefully close the connection
        CLEAR = 5,          // Clear the queue of commands
        EXECUTE_AT = 6,     // Run the queue of commands, starting at an index
        LOG = 7,            // Log a message
        FAIL = 8            // Report a failure to execute a peer's request
    };

    /// <summary>
    /// The meaning of the Message contents depend on the MessageType.
    /// </summary>
    /// <remarks>
    /// EXECUTE
    /// CommandSetId: The ID of the command set to execute
    /// 
    /// NEWCOMMAND
    /// CommandSetId: The ID number that represents the entire queue of commands
    /// Sequence: The total number of commands that will be in this set
    /// 
    /// COMMAND
    /// CommandSetId: The ID number that represents the entire queue of commands
    /// Sequence: The sequence number of the command
    /// Data: The command to be queued up
    /// Parameters: The command's parameters, if there are any
    /// 
    /// CLOSE
    /// No values need to be filled out
    /// 
    /// CLEAR
    /// No values need to be filled out
    /// 
    /// EXECUTE_AT
    /// Sequence:  The sequence number to start from
    /// 
    /// LOG
    /// Data: The logging information to record
    /// </remarks>
    [Serializable]
    public struct Message
    {
        // override object.Equals.  Testing each member individually for equality.
        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            Message test = (Message)obj;
            if ((Type != test.Type)
                || (Sequence != test.Sequence)
                || (CommandSetId != test.CommandSetId)
                || (Data != test.Data))
            {
                return false;
            }

            if (!Parameters.SequenceEqual(test.Parameters))
            {
                return false;
            }

            return true;
        }

        public MessageType Type;
        public int CommandSetId;
        public int Sequence;
        public String Data;
        public List<String> Parameters;
    }
}
