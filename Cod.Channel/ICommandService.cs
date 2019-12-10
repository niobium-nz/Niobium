using System.Collections.Generic;

namespace Cod.Channel
{
    public interface ICommandService
    {
        IReadOnlyCollection<CommandExecutionRecord> Executing { get; }

        IReadOnlyCollection<CommandExecutionRecord> Executed { get; }

        ICommand Get(string commandID);

        void OnExecuting(CommandExecutionRecord commandExecutionRecord);

        void OnExecuted(CommandExecutionRecord commandExecutionRecord);
    }
}
