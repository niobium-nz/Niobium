using System.Globalization;

namespace Cod.Platform.Locking
{
    public class Impediment : ITrackable
    {
        [EntityKey(EntityKeyKind.PartitionKey)]
        public string PartitionKey { get; set; }

        [EntityKey(EntityKeyKind.RowKey)]
        public string RowKey { get; set; }

        [EntityKey(EntityKeyKind.Timestamp)]
        public DateTimeOffset? Timestamp { get; set; }

        [EntityKey(EntityKeyKind.ETag)]
        public string ETag { get; set; }

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
    }
}
