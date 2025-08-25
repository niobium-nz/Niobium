using System.Diagnostics.CodeAnalysis;

namespace Niobium
{
    public interface IErrorRetriever
    {
        bool TryGet(string key, [NotNullWhen(true)] out string? value);
    }
}
