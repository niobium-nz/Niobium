using System.Collections;

namespace Cod
{
    public class EntityBag<T> : IReadOnlyList<T>
    {
        private readonly IReadOnlyList<T> result;

        public EntityBag(IReadOnlyList<T> result, string continuationToken)
        {
            this.result = result;
            ContinuationToken = continuationToken;
        }

        public T this[int index] => result[index];

        public int Count => result.Count;

        public string ContinuationToken { get; }

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
