using System.Globalization;
using Microsoft.WindowsAzure.Storage.Table;

namespace Cod.Platform
{
    public class Impediment : TableEntity, IEntity
    {
        public string Policy { get; set; }

        public DateTimeOffset? Created { get; set; }

        public string GetCategory() => this.PartitionKey.Split('-')[1];

        public int GetCause() => Int32.Parse(this.RowKey, CultureInfo.InvariantCulture);

        public static string BuildPartitionKey(string id, string category) => FormattableString.Invariant($"{id}-{category}");

        public static string BuildRowKey(int cause) => cause.ToString(CultureInfo.InvariantCulture);
    }
}
