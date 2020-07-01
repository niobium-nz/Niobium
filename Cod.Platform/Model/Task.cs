using System;
using Microsoft.Azure.Cosmos.Table;

namespace Cod.Platform.Model
{
    public class Task : TableEntity, IEntity
    {
        public const string SMS = "SMS";

        public string Reference { get; set; }

        public DateTimeOffset? Created { get; set; }

        public string GetKind() => this.PartitionKey;

        public string GetCorrelation() => this.RowKey;

        public static string BuildPartitionKey(string kind) => kind.Trim().ToUpperInvariant();

        public static string BuildRowKey(string correlation) => correlation.Trim();
    }
}
