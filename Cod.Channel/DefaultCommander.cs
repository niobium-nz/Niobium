using System;
using System.Collections.Generic;
using System.Linq;

namespace Cod.Channel
{
    internal class DefaultCommander : ICommander
    {
        private const int MaxQueueSize = 20;
        private readonly List<CommandExecutionRecord> executingQueue;
        private readonly List<CommandExecutionRecord> executedQueue;
        private readonly IEnumerable<ICommand> commands;
        private readonly Dictionary<string, IReadOnlyCollection<string>> busyStatus;

        public DefaultCommander(IEnumerable<ICommand> commands)
        {
            this.busyStatus = new Dictionary<string, IReadOnlyCollection<string>>();
            this.executingQueue = new List<CommandExecutionRecord>();
            this.executedQueue = new List<CommandExecutionRecord>();
            this.commands = commands;
            foreach (var command in commands)
            {
                command.Initialize(this);
            }
        }

        public IReadOnlyCollection<CommandExecutionRecord> Executing => this.executingQueue;

        public IReadOnlyCollection<CommandExecutionRecord> Executed => this.executedQueue;

        public IReadOnlyDictionary<string, IReadOnlyCollection<string>> Busy => this.busyStatus;

        public ICommand Get(CommandID id)
        {
            var result = this.commands.Where(c => c.ID == id).ToList();
            if (result.Count == 0)
            {
                throw new NotSupportedException($"No command found with ID {id.Group}.{id.Name}");
            }
            if (result.Count > 1)
            {
                throw new NotSupportedException($"Multiple commands found with ID {id.Group}.{id.Name}");
            }
            return result[0];
        }

        public IEnumerable<ICommand> Get() => this.commands;

        public void OnExecuted(CommandExecutionRecord commandExecutionRecord)
        {
            var executing = this.executingQueue.SingleOrDefault(e => e.EventArgs.ExecutionID == commandExecutionRecord.EventArgs.ExecutionID);
            if (executing != null)
            {
                this.executingQueue.Remove(executing);
            }
            this.executedQueue.Add(commandExecutionRecord);
            while (this.executedQueue.Count > MaxQueueSize)
            {
                this.executedQueue.RemoveAt(0);
            }
        }

        public void OnExecuting(CommandExecutionRecord commandExecutionRecord)
        {
            this.executingQueue.Add(commandExecutionRecord);
            while (this.executingQueue.Count > MaxQueueSize)
            {
                this.executingQueue.RemoveAt(0);
            }
        }

        public IDisposable SetBusy(string group, string name)
        {
            if (!this.busyStatus.ContainsKey(group))
            {
                this.busyStatus.Add(group, new List<string>());
            }
            var list = (List<string>)this.busyStatus[group];
            if (!list.Contains(name))
            {
                list.Add(name);
            }
            return new BusyState(this, group, name);
        }

        public void UnsetBusy(string group, string name)
        {
            if (this.busyStatus.ContainsKey(group))
            {
                var list = (List<string>)this.busyStatus[group];
                if (list.Contains(name))
                {
                    list.Remove(name);

                    if (list.Count == 0)
                    {
                        this.busyStatus.Remove(group);
                    }
                }
            }
        }
    }
}
