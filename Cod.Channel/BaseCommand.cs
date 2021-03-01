using System;
using System.Threading.Tasks;

namespace Cod.Channel
{
    public abstract class BaseCommand<T> : BaseCommand, ICommand<T>
    {
        public abstract Task<CommandExecutionEventArgs> ExecuteAsync(T parameter);
    }

    public abstract class BaseCommand : ICommand
    {
        protected ICommander Commander { get; private set; }

        public abstract CommandID ID { get; }

        public event EventHandler CanExecuteChanged;

        public abstract Task<CommandExecutionEventArgs> ExecuteAsync(object parameter);

        protected virtual void OnExecuting(object sender, CommandExecutionEventArgs e)
            => this.Commander.OnExecuting(new CommandExecutionRecord(e));

        protected virtual void OnExecuted(object sender, CommandExecutionEventArgs e)
            => this.Commander.OnExecuted(new CommandExecutionRecord(e));

        public void Initialize(ICommander commander) => this.Commander = commander;

        public virtual bool CanExecute(object parameter) => true;

        public async void Execute(object parameter) => await this.ExecuteAsync(parameter);

        protected virtual void OnCanExecuteChanged(object sender, EventArgs e) => this.CanExecuteChanged?.Invoke(sender, e);
    }
}
