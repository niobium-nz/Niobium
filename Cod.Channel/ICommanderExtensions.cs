using System;
using System.Linq;

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

        public static bool IsBusy(this ICommander commander, string group)
            => commander.IsBusy(group, String.Empty);

        public static bool IsBusy(this ICommander commander, string group, string name)
            => commander.Busy.ContainsKey(group) && commander.Busy[group].Contains(name);

        public static IDisposable SetBusy(this ICommander commander, string group) => commander.SetBusy(group, String.Empty);

        public static void UnsetBusy(this ICommander commander, string group) => commander.UnsetBusy(group, String.Empty);
    }
}
