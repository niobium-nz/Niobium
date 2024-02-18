namespace Cod.Platform
{
    internal class InternalErrorRetriever : IErrorRetriever
    {
        public bool TryGet(string key, out string value)
        {
            return R.TryGet(key, out value);
        }
    }
}
