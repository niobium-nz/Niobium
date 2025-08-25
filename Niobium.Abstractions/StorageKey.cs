using System.Diagnostics.CodeAnalysis;

namespace Niobium
{
    [method: SetsRequiredMembers]
    public struct StorageKey(string partitionKey, string rowKey) : IEquatable<StorageKey>
    {
        public const string MinKey = "!";
        public const string MaxKey = "~";

        public required string PartitionKey { get; set; } = partitionKey;

        public required string RowKey { get; set; } = rowKey;

        public override bool Equals(object? obj)
        {
            return obj is StorageKey key && Equals(key);
        }

        public bool Equals(StorageKey other)
        {
            return PartitionKey == other.PartitionKey && RowKey == other.RowKey;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(PartitionKey, RowKey);
        }

        public static bool operator ==(StorageKey left, StorageKey right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(StorageKey left, StorageKey right)
        {
            return !(left == right);
        }

        public override string ToString()
        {
            return this.BuildFullID();
        }
    }
}
