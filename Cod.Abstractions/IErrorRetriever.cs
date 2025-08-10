using System.Diagnostics.CodeAnalysis;

namespace Cod
{
    public interface IErrorRetriever
    {
        bool TryGet(string key, [NotNullWhen(true)] out string? value);
    }
}
