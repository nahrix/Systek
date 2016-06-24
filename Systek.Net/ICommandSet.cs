using System.Collections.Generic;

namespace Systek.Net
{
    /// <summary>
    /// Defines a set of commands to be run sequentially.
    /// </summary>
    public interface ICommandSet
    {
        /// <summary>
        /// Describes whether the CommandSet has its entire sequence of commands defined.
        /// </summary>
        bool Complete { get; }

        /// <summary>
        /// The commands to be run in sequence.  Each command has a sequence number, starting at 1,
        /// to be run from the lowest sequence number to the highest sequence number.
        /// </summary>
        List<ICommand> Commands { get; }

        /// <summary>
        /// The unique ID of this CommandSet.  Is used to validate that the commands added are for the correct CommandSet.
        /// </summary>
        int ID { get; }

        /// <summary>
        /// The highest sequence number in this commandSet.
        /// </summary>
        int Sequence { get; }

        /// <summary>
        /// Adds a command to the set, after performing some validation checks.
        /// </summary>
        /// <param name="command">The command to be added to the set.</param>
        /// <returns></returns>
        bool AddCommand(ICommand command);

        /// <summary>
        /// Removes a command from the set, after performing some validation checks.
        /// </summary>
        /// <param name="sequence">The sequence number of the command to remove.</param>
        /// <returns></returns>
        bool RemoveCommand(int sequence);
    }
}
