using System.Threading.Tasks;

namespace Cod.Channel
{
    public interface ICommand
    {
        string CommandID { get; }

        bool CanExecute { get; }

        void Initialize(ICommandService commandService);

        Task ExecuteAsync(object parameter);
    }
}
