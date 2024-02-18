using Cod.Platform.Database;

namespace Cod.Platform.Tenants.Wechat
{
    public class WechatEntity : ICachableEntity
    {
        public string PartitionKey { get; set; }

        public string RowKey { get; set; }

        public string Value { get; set; }

        public string ETag { get; set; }

        public DateTimeOffset? Timestamp { get; set; }

        public DateTimeOffset? Created { get; set; }

        public IConvertible GetCache()
        {
            return RowKey == BuildAPITicketRowKey() ? Value : null;
        }

        public static string BuildAPITicketPartitionKey(string appID)
        {
            return appID.Trim();
        }

        public static string BuildAPITicketRowKey()
        {
            return "APITicket";
        }

        public static string BuildOpenIDPartitionKey(string appID)
        {
            return appID.Trim();
        }

        public static string BuildOpenIDRowKey(string authcode)
        {
            return authcode.Trim();
        }

        public DateTimeOffset GetExpiry(DateTimeOffset timeStart)
        {
            return timeStart.AddMinutes(100);
        }

        public void Initialize(string partitionKey, string rowKey, IConvertible value)
        {
            PartitionKey = partitionKey;
            RowKey = rowKey;
            Value = value.ToString();
        }
    }
}
