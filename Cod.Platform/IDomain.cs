using Microsoft.Extensions.Logging;

namespace Cod.Platform
{
    public interface IDomain
    {
        bool Initialized { get; }

        void Initialize(ILogger logger);

        void Initialize(string partitionKey, string rowkey, ILogger logger);
    }

    public interface IDomain<T> : IDomain
    {
        void Initialize(T model, ILogger logger);
    }
}
