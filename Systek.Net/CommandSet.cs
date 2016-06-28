﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Systek.Net
{
    /// <summary>
    /// Base implementation of Systek.Net.ICommandSet
    /// </summary>
    /// <seealso cref="Systek.Net.ICommandSet" />
    [Serializable]
    public class CommandSet : ICommandSet
    {
        /// <summary>
        /// Describes whether the CommandSet has its entire sequence of commands defined.
        /// </summary>
        public bool Complete { get; private set; }

        /// <summary>
        /// The commands to be run in sequence.  Each command has a sequence number, starting at 1,
        /// to be run from the lowest sequence number to the highest sequence number.
        /// </summary>
        public List<ICommand> Commands { get; private set; }

        /// <summary>
        /// The unique ID of this CommandSet.  Is used to validate that the commands that are added are
        /// for the right CommandSet.
        /// </summary>
        public int ID { get; private set; }

        /// <summary>
        /// The highest sequence number in this commandSet.
        /// </summary>
        public int Sequence { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandSet" /> class.
        /// </summary>
        /// <param name="id">The unique ID of this CommandSet.</param>
        /// <param name="sequence">The highest sequence number that this CommandSet will contain.</param>
        public CommandSet(int id, int sequence)
        {
            ID = id;
            Sequence = sequence;
            Commands = new List<ICommand>();
            Complete = false;
        }

        /// <summary>
        /// Adds a command to the set, after performing some validation checks.
        /// </summary>
        /// <param name="command">The command to be added to the set.</param>
        /// <returns></returns>
        public bool AddCommand(ICommand command)
        {
            // Check if the command to add is valid, and return false if not
            if ((command.CommandSetId != ID) || (command.Sequence > Sequence) || (command.Sequence <= 0))
            {
                return false;
            }

            // Check if the command has already been added, and return false if it has
            foreach (ICommand cmd in Commands)
            {
                if (cmd.Sequence == command.Sequence)
                {
                    return false;
                }
            }

            // Add the command, and return success
            Commands.Add(command);
            if (Commands.Count == Sequence)
            {
                Complete = true;
            }

            return true;
        }

        /// <summary>
        /// Removes a command from the set, after performing some validation checks.
        /// </summary>
        /// <param name="sequence">The sequence number of the command to remove.</param>
        /// <returns>true if a command was found and removed.  false if the command was not found.</returns>
        public bool RemoveCommand(int sequence)
        {
            // Look for a Command with a matching sequence number, and remove it if found
            foreach (ICommand cmd in Commands)
            {
                if (cmd.Sequence == sequence)
                {
                    Commands.Remove(cmd);
                    Complete = false;
                    return true;
                }
            }

            return false;
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
            if ((other == null) || (other.GetType() != typeof(CommandSet)))
            {
                return false;
            }

            CommandSet test = (CommandSet)other;

            // Comparison of primitives
            if ((Sequence != test.Sequence) || (Complete != test.Complete) || (ID != test.ID))
            {
                return false;
            }

            // Comparison of objects
            if ((Commands != null) && !Commands.SequenceEqual(test.Commands))
            {
                return false;
            }

            return true;
        }
    }
}