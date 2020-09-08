namespace Cod
{
    public interface IErrorRetriever
    {
        bool TryGet(string key, out string value);
    }
}
