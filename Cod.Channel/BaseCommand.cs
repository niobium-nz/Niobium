using System.Threading.Tasks;

namespace Cod.Channel
{
    public abstract class BaseCommand : ICommand
    {
        protected ICommander Commander { get; private set; }

        public abstract CommandID ID { get; }

        public virtual bool CanExecute => true;

        public abstract Task<object> ExecuteAsync(object parameter);

        protected virtual void OnExecuting(object sender, CommandExecutionEventArgs e)
            => this.Commander.OnExecuting(new CommandExecutionRecord(e));

        protected virtual void OnExecuted(object sender, CommandExecutionEventArgs e)
            => this.Commander.OnExecuted(new CommandExecutionRecord(e));

        public void Initialize(ICommander commander) => this.Commander = commander;
    }
}
