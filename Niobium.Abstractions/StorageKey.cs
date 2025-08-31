using System.Diagnostics.CodeAnalysis;

namespace Niobium
{
    [method: SetsRequiredMembers]
    public struct StorageKey(string partitionKey, string rowKey) : IEquatable<StorageKey>
    {
        private const char Separator = '%';
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
            return $"{PartitionKey}{Separator}{RowKey}";
        }

        public static bool TryParse(string fullID, [NotNullWhen(true)] out StorageKey? result)
        {
            string[] splited = fullID.Split(Separator, StringSplitOptions.RemoveEmptyEntries);
            if (splited.Length == 2)
            {
                result = new StorageKey { PartitionKey = splited[0], RowKey = splited[1] };
                return true;
            }
            else
            {
                result = null;
                return false;
            }

        }

        public static StorageKey Parse(string fullID)
        {
            return !TryParse(fullID, out StorageKey? result)
                ? throw new InvalidDataException($"Invalid data found for {nameof(StorageKey)}: {fullID}")
                : result.Value;
        }
    }
}
