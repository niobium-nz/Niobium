using System.Threading.Tasks;

namespace Cod.Channel
{
    public static class ICommandExtensions
    {
        public static async Task<object> ExecuteAsync(this ICommand command)
            => await command.ExecuteAsync(null);

        public static async Task<OperationResult<T>> ExecuteAsync<T>(this ICommand command)
            => (OperationResult<T>)await command.ExecuteAsync(null);


        public static async Task<OperationResult<TReturn>> ExecuteAsync<TReturn, TParameter>(this ICommand command, TParameter parameter)
            => (OperationResult<TReturn>)await command.ExecuteAsync(parameter);
    }
}
