using System.Threading.Tasks;

namespace Cod.Channel
{
    public abstract class GenericCommand : BaseCommand, ICommand
    {
        public override async Task ExecuteAsync(object parameter)
        {
            var args = this.BuildEventArgs();
            this.OnExecuting(this, args);
            var result = await this.CoreExecuteAsync();
            var executedArgs = (CommandExecutionEventArgs)args.Clone();
            executedArgs.Result = result; 
            this.OnExecuted(this, executedArgs);
        }

        protected abstract Task<OperationResult> CoreExecuteAsync();

        protected virtual CommandExecutionEventArgs BuildEventArgs() => new CommandExecutionEventArgs(this.ID, null);
    }

    public abstract class GenericCommand<T> : BaseCommand, ICommand
    {
        public override async Task ExecuteAsync(object parameter)
        {
            if (parameter is T p)
            {
                var args = this.BuildEventArgs(p);
                this.OnExecuting(this, args);
                var result = await this.CoreExecuteAsync(p);
                var executedArgs = (CommandExecutionEventArgs)args.Clone();
                executedArgs.Result = result;
                this.OnExecuted(this, executedArgs);
            }
        }

        protected abstract Task<OperationResult> CoreExecuteAsync(T parameter);

        protected virtual CommandExecutionEventArgs BuildEventArgs(T parameter) => new CommandExecutionEventArgs(this.ID, parameter);
    }
}
