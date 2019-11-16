using System;
using Cod.Contract;
using Microsoft.WindowsAzure.Storage.Table;

namespace Cod.Platform
{
    public class Transaction : TableEntity
    {
        public double Delta { get; set; }

        public int Reason { get; set; }

        public string Remark { get; set; }

        public string Reference { get; set; }

        public void SetOwner(string value) => this.PartitionKey = BuildPartitionKey(value);

        public string GetOwner() => this.PartitionKey;

        public DateTimeOffset GetCreated() => ParseRowKey(this.RowKey);

        public void SetCreated(DateTimeOffset created) => this.RowKey = BuildRowKey(created);

        public static string BuildPartitionKey(string owner) => owner.Trim();

        public static string BuildRowKey(DateTimeOffset created) => created.ToReverseUnixTimestamp();

        public static string BuildRowKey(long created) => DateTimeOffsetExtensions.FromReverseUnixTimeMilliseconds(created).ToReverseUnixTimestamp();

        public static DateTimeOffset ParseRowKey(string input) => DateTimeOffsetExtensions.FromReverseUnixTimeMilliseconds(Int64.Parse(input));
    }
}