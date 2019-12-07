using System;
using System.Linq;
using System.Threading.Tasks;

namespace Cod.Channel
{
    public abstract class GenericCommand : BaseCommand, ICommand
    {
        public override async Task ExecuteAsync(object parameter)
        {
            var args = this.BuildEventArgs();
            this.OnExecuting(this, args);
            await this.CoreExecuteAsync();
            this.OnExecuted(this, args);
        }

        protected abstract Task CoreExecuteAsync();

        protected virtual CommandExecutionEventArgs BuildEventArgs() => new CommandExecutionEventArgs(String.Empty, Enumerable.Empty<object>());
    }

    public abstract class GenericCommand<T> : BaseCommand, ICommand
    {
        public override async Task ExecuteAsync(object parameter)
        {
            if (parameter is T p)
            {
                var args = this.BuildEventArgs(p);
                this.OnExecuting(this, args);
                await this.CoreExecuteAsync(p);
                this.OnExecuted(this, args);
            }
        }

        protected abstract Task CoreExecuteAsync(T parameter);

        protected virtual CommandExecutionEventArgs BuildEventArgs(T parameter) => new CommandExecutionEventArgs(String.Empty, Enumerable.Empty<object>());
    }
}
