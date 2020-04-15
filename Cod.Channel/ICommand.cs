using System.Threading.Tasks;

namespace Cod.Channel
{
    public interface ICommand<T>
    {
        CommandID ID { get; }

        bool CanExecute { get; }

        void Initialize(ICommander commander);

        Task<CommandExecutionEventArgs> ExecuteAsync(T parameter);
    }

    public interface ICommand
    {
        CommandID ID { get; }

        bool CanExecute { get; }

        void Initialize(ICommander commander);

        Task<CommandExecutionEventArgs> ExecuteAsync(object parameter);
    }
}
