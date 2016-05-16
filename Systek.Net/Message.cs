using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Systek.Net
{
    /// <summary>
    /// Describes the kind of Message that is being sent.
    /// </summary>
    [Serializable]
    public enum MessageType
    {
        EXECUTE = 1,        // Run any queued commands
        COMMAND = 2,        // Queue a command
        CLOSE = 3,          // Gracefully close the connection
        CLEAR = 4,          // Clear the queue of commands
        EXECUTE_AT = 5,     // Run the queue of commands, starting at an index
        LOG = 6             // Log a message
    };

    /// <summary>
    /// The meaning of the Message contents depend on the MessageType.
    /// </summary>
    /// <remarks>
    /// EXECUTE
    /// No values need to be filled out
    /// 
    /// COMMAND
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
        // override object.Equals
        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            Message test = (Message)obj;
            if ((Type != test.Type)
                || (Sequence != test.Sequence)
                || (Data != test.Data))
            {
                return false;
            }

            if ((Parameters.Count != test.Parameters.Count)
                || Parameters.Except(test.Parameters).Any())
            {
                return false;
            }

            return true;
        }

        public MessageType Type;
        public int Sequence;
        public String Data;
        public List<String> Parameters;
    }
}
