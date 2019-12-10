using System.Threading.Tasks;

namespace Cod.Channel
{
    public abstract class BaseCommand : ICommand
    {
        protected ICommandService CommandService { get; private set; }

        public abstract string CommandID { get; }

        public virtual bool CanExecute => true;

        public abstract Task ExecuteAsync(object parameter);

        protected virtual void OnExecuting(object sender, CommandExecutionEventArgs e)
            => this.CommandService.OnExecuting(new CommandExecutionRecord(e));

        protected virtual void OnExecuted(object sender, CommandExecutionEventArgs e)
            => this.CommandService.OnExecuted(new CommandExecutionRecord(e));

        public void Initialize(ICommandService commandService) => this.CommandService = commandService;
    }
}
