using System;
using Microsoft.Azure.Cosmos.Table;

namespace Cod.Platform
{
    public class Impediment : TableEntity, IEntity
    {
        public string Policy { get; set; }

        public DateTimeOffset? Created { get; set; }

        public string GetCategory() => this.PartitionKey.Split('-')[1];

        public int GetCause() => Int32.Parse(this.RowKey);

        public static string BuildPartitionKey(string id, string category) => String.Format("{0}-{1}", id, category);

        public static string BuildRowKey(int cause) => cause.ToString();
    }
}
