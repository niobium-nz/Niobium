using System;

namespace Cod.Platform.Model
{
    public class WechatEntity : ICachableEntity
    {
        public string PartitionKey { get; set; }

        public string RowKey { get; set; }

        public string Value { get; set; }

        public IConvertible GetCache() => this.RowKey == BuildAPITicketRowKey() ? this.Value : null;

        public static string BuildAPITicketPartitionKey(string appID) => appID.Trim();

        public static string BuildAPITicketRowKey() => "APITicket";

        public static string BuildOpenIDPartitionKey(string appID) => appID.Trim();

        public static string BuildOpenIDRowKey(string authcode) => authcode.Trim();

        public DateTimeOffset GetExpiry(DateTimeOffset timeStart) => timeStart.AddMinutes(100);

        public void Initialize(string partitionKey, string rowKey, IConvertible value)
        {
            this.PartitionKey = partitionKey;
            this.RowKey = rowKey;
            this.Value = value.ToString();
        }
    }
}
