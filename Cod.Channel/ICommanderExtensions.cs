namespace Cod.Channel
{
    public static class ILoadingStateServiceExtensions
    {
        private static readonly string groupBusyPlaceholder = "$_$GROUP_BUSY_PLACEHOLDER$_$";

        public static bool IsBusy(this ILoadingStateService service, string group)
        {
            return service.State.ContainsKey(group);
        }

        public static bool IsBusy(this ILoadingStateService service, string group, string name)
        {
            return service.State.ContainsKey(group) && service.State[group].Contains(name);
        }

        public static IDisposable SetBusy(this ILoadingStateService service, string group)
        {
            return service.SetBusy(group, groupBusyPlaceholder);
        }

        public static void UnsetBusy(this ILoadingStateService service, string group)
        {
            service.UnsetBusy(group);
        }
    }
}
