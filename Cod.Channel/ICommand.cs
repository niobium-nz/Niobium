using System.Threading.Tasks;

namespace Cod.Channel
{
    public interface ICommand<T> : ICommand
    {
        Task<CommandExecutionEventArgs> ExecuteAsync(T parameter);
    }

    public interface ICommand : System.Windows.Input.ICommand
    {
        CommandID ID { get; }

        void Initialize(ICommander commander);

        Task<CommandExecutionEventArgs> ExecuteAsync(object parameter);
    }
}
