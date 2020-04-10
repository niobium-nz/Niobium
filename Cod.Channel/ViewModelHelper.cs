using System;
using System.Threading.Tasks;

namespace Cod.Channel
{
    public static class ViewModelHelper
    {
        public async static Task ValidateAndExecuteAsync(
            Func<Task<ICommand>> getCommand,
            Func<Task<object>> getCommandParameter,
            Func<ValidationState, Task> setValidationState = null,
            Func<string, Task> setErrorMessage = null)
        {
            var command = await getCommand();
            var parameter = await getCommandParameter();
            if (command != null && parameter != null)
            {
                var result = await command.ExecuteAsync(parameter);
                if (result.IsSuccess)
                {
                    if (setValidationState != null)
                    {
                        await setValidationState(null);
                    }
                }
                else
                {
                    if (setErrorMessage != null)
                    {
                        await setErrorMessage(result.Message);
                    }
                    if (result.Code == InternalError.BadRequest)
                    {
                        if (setValidationState != null)
                        {
                            await setValidationState(result.Reference as ValidationState);
                        }
                    }
                }
            }
        }
    }
}
