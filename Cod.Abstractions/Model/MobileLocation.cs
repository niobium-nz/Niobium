using System;

namespace Cod.Model
{
    public class MobileLocation : IEntity
    {
        public string PartitionKey { get; set; }

        public string RowKey { get; set; }

        public DateTimeOffset? Timestamp { get; set; }

        public string ETag { get; set; }

        public string Province { get; set; }

        public string City { get; set; }

        public string Isp { get; set; }

        public DateTimeOffset? Created { get; set; }

        public static string BuildPartitionKey(string mobile)
        {
            return mobile.Trim().Substring(0, 2);
        }

        public static string BuildRowKey(string mobile)
        {
            return mobile.Trim().Substring(2, 5);
        }
    }
}
