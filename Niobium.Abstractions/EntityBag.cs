using System.Collections;

namespace Niobium
{
    public class EntityBag<T>(IReadOnlyList<T> result, string? continuationToken) : IReadOnlyList<T>
    {
        public T this[int index] => result[index];

        public int Count => result.Count;

        public string? ContinuationToken { get; } = continuationToken;

        public bool Contains(T item)
        {
            return result.Contains(item);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return result.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return result.GetEnumerator();
        }
    }
}
