using System.Diagnostics.CodeAnalysis;

namespace Cod
{
    public interface IDomain
    {
        string? PartitionKey { get; }

        string? RowKey { get; }

        [MemberNotNullWhen(true, nameof(PartitionKey), nameof(RowKey))]
        bool Initialized { get; }

        Task<string?> GetHashAsync(CancellationToken cancellationToken = default);
    }

    public interface IDomain<T> : IDomain
    {
        Task<T?> TryGetEntityAsync(CancellationToken cancellationToken = default);

        Task<T> GetEntityAsync(CancellationToken cancellationToken = default);

        IDomain<T> Initialize(T entity);

        IDomain<T> Initialize(string partitionKey, string rowKey);
    }
}
