using System.Collections.Concurrent;

namespace Cod.Platform
{
    internal static class CallContext<T>
    {
        private static readonly ConcurrentDictionary<string, AsyncLocal<T>> state = new ConcurrentDictionary<string, AsyncLocal<T>>();

        public static void SetData(string name, T data) =>
            state.GetOrAdd(name, _ => new AsyncLocal<T>()).Value = data;

        public static T GetData(string name) =>
            state.TryGetValue(name, out var data) ? data.Value : default;
    }
}
