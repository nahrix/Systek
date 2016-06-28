﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Systek.Net
{
    /// <summary>
    /// Base implementation of Systek.Net.ICommand
    /// </summary>
    /// <seealso cref="Systek.Net.ICommand" />
    [Serializable]
    public class Command : ICommand
    {
        /// <summary>
        /// The ID of the CommandSet that this command is a member of.
        /// </summary>
        public int CommandSetId { get; private set; }

        /// <summary>
        /// The position in the queue of commands to be run within the CommandSet.
        /// </summary>
        public int Sequence { get; private set; }

        /// <summary>
        /// The command to be executed
        /// </summary>
        public string Cmd { get; private set; }

        /// <summary>
        /// The parameters for the command to be executed, if any.
        /// </summary>
        public List<string> Parameters { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Command" /> class.
        /// </summary>
        /// <param name="cmdSetId">The command set identifier.</param>
        /// <param name="seq">The sequence number of this command.</param>
        /// <param name="cmd">The command to be executed.</param>
        /// <param name="param">The parameters for the command, if any.</param>
        public Command(int cmdSetId, int seq, string cmd, List<string> param = null)
        {
            CommandSetId = cmdSetId;
            Sequence = seq;
            Cmd = cmd;
            Parameters = new List<string>();

            if (param != null)
            {
                foreach (string value in param)
                {
                    Parameters.Add(value);
                }
            }
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object" />, is equal to this instance.
        /// </summary>
        /// <param name="other">The <see cref="System.Object" /> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="System.Object" /> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object other)
        {
            // Type check
            if ((other == null) || (other.GetType() != typeof(Command)))
            {
                return false;
            }

            Command test = (Command)other;

            // Comparison of primitives
            if ((CommandSetId != test.CommandSetId) || (Sequence != test.Sequence) || (Cmd != test.Cmd)
                || !Parameters.SequenceEqual(test.Parameters))
            {
                return false;
            }

            return true;
        }
    }
}