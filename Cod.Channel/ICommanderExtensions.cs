using System;

namespace Cod.Channel
{
    public static class ICommanderExtensions
    {
        public static void SetBusy(this ICommander commander, string group) => commander.SetBusy(group, String.Empty);

        public static void UnsetBusy(this ICommander commander, string group) => commander.UnsetBusy(group, String.Empty);
    }
}
