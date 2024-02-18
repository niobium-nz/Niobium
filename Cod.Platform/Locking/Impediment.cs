using Azure;
using Azure.Data.Tables;
using System.Globalization;

namespace Cod.Platform.Locking
{
    public class Impediment : IEntity, ITableEntity
    {
        public string PartitionKey { get; set; }

        public string RowKey { get; set; }

        public string ETag { get; set; }

        public DateTimeOffset? Timestamp { get; set; }

        public DateTimeOffset? Created { get; set; }

        public string Policy { get; set; }

        public string GetCategory()
        {
            return PartitionKey.Split('-')[1];
        }

        public int GetCause()
        {
            return int.Parse(RowKey, CultureInfo.InvariantCulture);
        }

        public static string BuildPartitionKey(string id, string category)
        {
            return FormattableString.Invariant($"{id}-{category}");
        }

        public static string BuildRowKey(int cause)
        {
            return cause.ToString(CultureInfo.InvariantCulture);
        }

        ETag ITableEntity.ETag { get => new(ETag); set => ETag = value.ToString(); }
    }
}
