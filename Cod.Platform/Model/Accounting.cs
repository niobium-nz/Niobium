using System;
using Microsoft.WindowsAzure.Storage.Table;

namespace Cod.Platform.Model
{
    public class Accounting : TableEntity
    {
        public double Credits { get; set; }

        public double Debits { get; set; }

        public double Balance { get; set; }

        public string GetPrincipal() => this.PartitionKey;

        public void SetPrincipal(string value) => this.PartitionKey = BuildPartitionKey(value);

        public DateTimeOffset GetEnd() => DateTimeOffsetExtensions.FromReverseUnixTimeMilliseconds(Int64.Parse(this.RowKey));

        public void SetEnd(DateTimeOffset value) => this.RowKey = BuildRowKey(value);

        public static string BuildPartitionKey(string principal) => principal.Trim();

        public static string BuildRowKey(DateTimeOffset end) => end.ToReverseUnixTimestamp();
    }
}