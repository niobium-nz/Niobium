using System;

namespace Cod.Channel
{
    public static class ICommanderExtensions
    {
        public static ICommand<TParameter> Get<TParameter>(this ICommander commander, CommandID id)
        {
            var command = commander.Get(id);
            if (command != null && command is ICommand<TParameter> result)
            {
                return result;
            }
            return null;
        }

        public static IDisposable SetBusy(this ICommander commander, string group) => commander.SetBusy(group, String.Empty);

        public static void UnsetBusy(this ICommander commander, string group) => commander.UnsetBusy(group, String.Empty);
    }
}
