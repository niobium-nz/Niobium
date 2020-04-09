using System.Threading.Tasks;

namespace Cod.Channel
{
    public interface ICommand
    {
        CommandID ID { get; }

        bool CanExecute { get; }

        void Initialize(ICommander commander);

        Task<OperationResult> ExecuteAsync(object parameter);
    }
}
