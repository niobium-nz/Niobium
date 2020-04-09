using System.Threading.Tasks;

namespace Cod.Channel
{
    public abstract class GenericCommand : BaseCommand, ICommand
    {
        public override async Task<object> ExecuteAsync(object parameter)
        {
            var args = this.BuildEventArgs();
            this.OnExecuting(this, args);
            var result = await this.CoreExecuteAsync();
            var executedArgs = (CommandExecutionEventArgs)args.Clone();
            executedArgs.Result = result;
            this.OnExecuted(this, executedArgs);
            return result;
        }

        protected abstract Task<OperationResult> CoreExecuteAsync();

        protected virtual CommandExecutionEventArgs BuildEventArgs() => new CommandExecutionEventArgs(this.ID, null);
    }

    public abstract class GenericCommand<TReturn> : BaseCommand, ICommand
    {
        public override async Task<object> ExecuteAsync(object parameter)
        {
            var args = this.BuildEventArgs();
            this.OnExecuting(this, args);
            var result = await this.CoreExecuteAsync();
            var executedArgs = (CommandExecutionEventArgs)args.Clone();
            executedArgs.Result = result;
            this.OnExecuted(this, executedArgs);
            return result.Result;
        }

        protected abstract Task<OperationResult<TReturn>> CoreExecuteAsync();

        protected virtual CommandExecutionEventArgs BuildEventArgs() => new CommandExecutionEventArgs(this.ID, null);
    }

    public abstract class GenericCommand<TParameter, TReturn> : BaseCommand, ICommand
    {
        public override async Task<object> ExecuteAsync(object parameter)
        {
            if (parameter is TParameter p)
            {
                var args = this.BuildEventArgs(p);
                this.OnExecuting(this, args);
                var result = await this.CoreExecuteAsync(p);
                var executedArgs = (CommandExecutionEventArgs)args.Clone();
                executedArgs.Result = result;
                this.OnExecuted(this, executedArgs);
                return result.Result;
            }

            return null;
        }

        protected abstract Task<OperationResult<TReturn>> CoreExecuteAsync(TParameter parameter);

        protected virtual CommandExecutionEventArgs BuildEventArgs(TParameter parameter) => new CommandExecutionEventArgs(this.ID, parameter);
    }
}
