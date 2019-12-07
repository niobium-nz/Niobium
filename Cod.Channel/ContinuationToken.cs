using System;
using System.Collections.Generic;

namespace Cod.Channel
{
    public class ContinuationToken : IEquatable<ContinuationToken>
    {
        public string NextPartitionKey { get; set; }

        public string NextRowKey { get; set; }

        public override bool Equals(object obj) => obj is ContinuationToken token && this.NextPartitionKey == token.NextPartitionKey && this.NextRowKey == token.NextRowKey;

        public bool Equals(ContinuationToken other) => this.NextPartitionKey == other.NextPartitionKey && this.NextRowKey == other.NextRowKey;

        public override int GetHashCode()
        {
            var hashCode = 1667455016;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(this.NextPartitionKey);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(this.NextRowKey);
            return hashCode;
        }

        public static bool operator ==(ContinuationToken left, ContinuationToken right) => EqualityComparer<ContinuationToken>.Default.Equals(left, right);
        public static bool operator !=(ContinuationToken left, ContinuationToken right) => !(left == right);
    }
}
