namespace Niobium.Channel
{
    internal sealed class DefaultLoadingStateService : ILoadingStateService
    {
        private readonly Dictionary<string, IReadOnlyCollection<string>> state = [];

        public IReadOnlyDictionary<string, IReadOnlyCollection<string>> State => state;

        public IDisposable SetBusy(string group, string name)
        {
            if (!state.TryGetValue(group, out IReadOnlyCollection<string>? values))
            {
                values = new List<string>();
                state.Add(group, values);
            }
            List<string> list = (List<string>)values;
            if (!list.Contains(name))
            {
                list.Add(name);
            }
            return new BusyState(this, group, name);
        }

        public void UnsetBusy(string group, string name)
        {
            if (state.TryGetValue(group, out IReadOnlyCollection<string>? value))
            {
                List<string> list = (List<string>)value;
                list.Remove(name);

                if (list.Count == 0)
                {
                    state.Remove(group);
                }
            }
        }

        public void UnsetBusy(string group)
        {
            state.Remove(group);
        }
    }
}
