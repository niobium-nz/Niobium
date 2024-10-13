namespace Cod.Channel
{
    public interface ILoadingStateService
    {
        IReadOnlyDictionary<string, IReadOnlyCollection<string>> State { get; }

        IDisposable SetBusy(string group, string name);

        void UnsetBusy(string group, string name);
    }
}
