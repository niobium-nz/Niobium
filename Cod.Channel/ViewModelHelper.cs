using System;
using System.Threading.Tasks;

namespace Cod.Channel
{
    public static class ViewModelHelper
    {
        public static async Task ValidateAndExecuteAsync(
            Func<Task<ICommand>> getCommand,
            Func<Task<object>> getCommandParameter,
            Func<CommandExecutionEventArgs, Task> onSuccess = null,
            Func<CommandExecutionEventArgs, Task> onError = null)
        {
            var command = await getCommand();
            var parameter = await getCommandParameter();
            if (command != null && parameter != null)
            {
                var result = await command.ExecuteAsync(parameter);
                if (result.Result.IsSuccess)
                {
                    if (onSuccess != null)
                    {
                        await onSuccess(result);
                    }
                }
                else
                {
                    if (onError != null)
                    {
                        await onError(result);
                    }
                }
            }
        }
    }
}
