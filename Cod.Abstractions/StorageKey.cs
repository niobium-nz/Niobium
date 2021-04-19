using System;
using System.Collections.Generic;

namespace Cod
{
    public struct StorageKey : IEquatable<StorageKey>
    {
        public const string MinKey = "!";
        public const string MaxKey = "~";

        public string PartitionKey { get; set; }

        public string RowKey { get; set; }

        public override bool Equals(object obj) => obj is StorageKey key && this.Equals(key);
        public bool Equals(StorageKey other) => this.PartitionKey == other.PartitionKey && this.RowKey == other.RowKey;

        public override int GetHashCode()
        {
            var hashCode = 1963138530;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(this.PartitionKey);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(this.RowKey);
            return hashCode;
        }

        public static bool operator ==(StorageKey left, StorageKey right) => left.Equals(right);
        public static bool operator !=(StorageKey left, StorageKey right) => !(left == right);
    }
}
