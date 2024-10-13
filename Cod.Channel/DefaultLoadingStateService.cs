namespace Cod.Channel
{
    internal class DefaultLoadingStateService : ILoadingStateService
    {
        private readonly Dictionary<string, IReadOnlyCollection<string>> state = [];

        public IReadOnlyDictionary<string, IReadOnlyCollection<string>> State => this.state;

        public IDisposable SetBusy(string group, string name)
        {
            if (!state.TryGetValue(group, out var values))
            {
                values = new List<string>();
                this.state.Add(group, values);
            }
            var list = (List<string>)values;
            if (!list.Contains(name))
            {
                list.Add(name);
            }
            return new BusyState(this, group, name);
        }

        public void UnsetBusy(string group, string name)
        {
            if (state.TryGetValue(group, out var value))
            {
                var list = (List<string>)value;
                list.Remove(name);

                if (list.Count == 0)
                {
                    this.state.Remove(group);
                }
            }
        }
    }
}
