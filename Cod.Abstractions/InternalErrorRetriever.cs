namespace Cod
{
    internal class InternalErrorRetriever : IErrorRetriever
    {
        public bool TryGet(string key, out string value)
        {
            return R.TryGet(key, out value);
        }
    }
}
