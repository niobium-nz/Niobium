using System;

namespace Cod
{
    public class Entitlement : IEntity
    {
        public string PartitionKey { get; set; }

        public string RowKey { get; set; }

        public DateTimeOffset Timestamp { get; set; }

        public string ETag { get; set; }

        public string Value { get; set; }

        public DateTimeOffset? Created { get; set; }
 
        public static string BuildPartitionKey(OpenIDProvider provider, string appID, string accountID)
        {
            if (appID is null)
            {
                throw new ArgumentNullException(nameof(appID));
            }

            if (accountID is null)
            {
                throw new ArgumentNullException(nameof(accountID));
            }

            return $"{(int)provider}|{appID.Trim()}|{accountID.Trim()}";
        }

        public string GetFullAccountID() => this.PartitionKey.Trim();

        public string GetKey() => this.RowKey.Trim();
    }
}
