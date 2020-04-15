using System.Threading.Tasks;

namespace Cod.Channel
{
    public static class ICommandExtensions
    {
        public static async Task<CommandExecutionEventArgs> ExecuteAsync(this ICommand command)
            => await command.ExecuteAsync(null);
    }
}
