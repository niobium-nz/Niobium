namespace Cod
{
    internal class InternalErrorRetriever : IErrorRetriever
    {
        public bool TryGet(string key, out string value) => R.TryGet(key, out value);
    }
}
