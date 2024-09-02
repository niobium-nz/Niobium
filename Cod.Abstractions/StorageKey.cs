namespace Cod
{
    public struct StorageKey : IEquatable<StorageKey>
    {
        public const string MinKey = "!";
        public const string MaxKey = "~";

        public string PartitionKey { get; set; }

        public string RowKey { get; set; }

        public override bool Equals(object obj)
        {
            return obj is StorageKey key && Equals(key);
        }

        public bool Equals(StorageKey other)
        {
            return PartitionKey == other.PartitionKey && RowKey == other.RowKey;
        }

        public override int GetHashCode()
        {
            int hashCode = 1963138530;
            hashCode = (hashCode * -1521134295) + EqualityComparer<string>.Default.GetHashCode(PartitionKey);
            hashCode = (hashCode * -1521134295) + EqualityComparer<string>.Default.GetHashCode(RowKey);
            return hashCode;
        }

        public static bool operator ==(StorageKey left, StorageKey right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(StorageKey left, StorageKey right)
        {
            return !(left == right);
        }
    }
}
