using System;
using System.Threading.Tasks;

namespace Cod.Channel
{
    public abstract class BaseCommand : ICommand
    {
        public abstract string CommandID { get; }

        public virtual bool CanExecute => true;

        public event EventHandler<CommandExecutionEventArgs> Executing;

        public event EventHandler<CommandExecutionEventArgs> Executed;

        public abstract Task ExecuteAsync(object parameter);

        protected virtual void OnExecuting(object sender, CommandExecutionEventArgs e) => Executing?.Invoke(sender, e);

        protected virtual void OnExecuted(object sender, CommandExecutionEventArgs e) => Executed?.Invoke(sender, e);
    }
}
