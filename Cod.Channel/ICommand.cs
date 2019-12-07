using System;
using System.Threading.Tasks;

namespace Cod.Channel
{
    public interface ICommand
    {
        string CommandID { get; }

        bool CanExecute { get; }

        event EventHandler<CommandExecutionEventArgs> Executing;

        event EventHandler<CommandExecutionEventArgs> Executed;

        Task ExecuteAsync(object parameter);
    }
}
