using System.Diagnostics.CodeAnalysis;

namespace Niobium
{
    internal sealed class InternalErrorRetriever : IErrorRetriever
    {
        public bool TryGet(string key, [NotNullWhen(true)] out string? value)
        {
            return R.TryGet(key, out value);
        }
    }
}
