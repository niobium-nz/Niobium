namespace Cod.Channel
{
    public static class ILoadingStateServiceExtensions
    {
        public static bool IsBusy(this ILoadingStateService service, string group)
            => service.IsBusy(group, String.Empty);

        public static bool IsBusy(this ILoadingStateService service, string group, string name)
            => service.State.ContainsKey(group) && service.State[group].Contains(name);

        public static IDisposable SetBusy(this ILoadingStateService service, string group) => service.SetBusy(group, String.Empty);

        public static void UnsetBusy(this ILoadingStateService service, string group) => service.UnsetBusy(group, String.Empty);
    }
}
