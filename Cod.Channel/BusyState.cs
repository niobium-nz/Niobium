namespace Cod.Channel
{
    internal class BusyState(ILoadingStateService service, string group, string name) : IDisposable
    {
        public void Dispose() => service.UnsetBusy(group, name);
    }
}
