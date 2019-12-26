using System.Collections.Generic;

namespace Cod.Channel
{
    public interface ICommander
    {
        IReadOnlyDictionary<string, IReadOnlyCollection<string>> Busy { get; }

        IReadOnlyCollection<CommandExecutionRecord> Executing { get; }

        IReadOnlyCollection<CommandExecutionRecord> Executed { get; }

        ICommand Get(CommandID id);

        IEnumerable<ICommand> Get();

        void SetBusy(string group, string name);

        void UnsetBusy(string group, string name);

        void OnExecuting(CommandExecutionRecord commandExecutionRecord);

        void OnExecuted(CommandExecutionRecord commandExecutionRecord);
    }
}
