using System.Collections.Generic;
using System.Linq;

namespace Cod.Channel
{
    internal class DefaultCommandService : ICommandService
    {
        private const int MaxQueueSize = 20;
        private readonly Queue<CommandExecutionRecord> executingQueue;
        private readonly Queue<CommandExecutionRecord> executedQueue;
        private readonly IEnumerable<ICommand> commands;

        public DefaultCommandService(IEnumerable<ICommand> commands)
        {
            this.executingQueue = new Queue<CommandExecutionRecord>();
            this.executedQueue = new Queue<CommandExecutionRecord>();
            this.commands = commands;
            foreach (var command in commands)
            {
                command.Initialize(this);
            }
        }

        public IReadOnlyCollection<CommandExecutionRecord> Executing => this.executingQueue;

        public IReadOnlyCollection<CommandExecutionRecord> Executed => this.executedQueue;

        public ICommand Get(string commandID) => this.commands.SingleOrDefault(c => c.CommandID == commandID);

        public void OnExecuted(CommandExecutionRecord commandExecutionRecord)
        {
            this.executedQueue.Enqueue(commandExecutionRecord);
            while (this.executedQueue.Count > MaxQueueSize)
            {
                this.executedQueue.Dequeue();
            }
        }

        public void OnExecuting(CommandExecutionRecord commandExecutionRecord)
        {
            this.executingQueue.Enqueue(commandExecutionRecord);
            while (this.executingQueue.Count > MaxQueueSize)
            {
                this.executingQueue.Dequeue();
            }
        }
    }
}
