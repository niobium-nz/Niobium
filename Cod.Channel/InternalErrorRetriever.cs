using System.Diagnostics.CodeAnalysis;

namespace Cod.Channel
{
    internal class InternalErrorRetriever : IErrorRetriever
    {
        public bool TryGet(string key, [NotNullWhen(true)] out string? value) => R.TryGet(key, out value);
    }
}
