using System;
using Microsoft.WindowsAzure.Storage.Table;

namespace Cod.Platform.Model
{
    public class WechatEntity : TableEntity, ICachableEntity
    {
        public string Value { get; set; }

        public IConvertible GetCache() => this.RowKey == BuildAPITicketRowKey() ? this.Value : null;

        public void SetCache(IConvertible value)
        {
            if (this.RowKey == BuildAPITicketRowKey())
            {
                this.Value = value.ToString();
            }
        }

        public static string BuildAPITicketPartitionKey(string appID) => appID.Trim();

        public static string BuildAPITicketRowKey() => "APITicket";

        public static string BuildOpenIDPartitionKey(string appID) => appID.Trim();

        public static string BuildOpenIDRowKey(string authcode) => authcode.Trim();

        public DateTimeOffset GetExpiry(DateTimeOffset timeStart)
        {
            return timeStart.AddMinutes(100);
        }
    }
}
