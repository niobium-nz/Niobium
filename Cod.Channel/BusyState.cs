namespace Cod.Channel
{
    internal sealed class BusyState(ILoadingStateService service, string group, string name) : IDisposable
    {
        public void Dispose()
        {
            service.UnsetBusy(group, name);
        }
    }
}
