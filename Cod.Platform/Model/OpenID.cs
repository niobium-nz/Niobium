using System;
using Microsoft.Azure.Cosmos.Table;

namespace Cod.Platform.Model
{
    public class OpenID : TableEntity
    {
        public string AppID { get; set; }

        public string UserID { get; set; }

        public OpenIDProvider GetProvider() => (OpenIDProvider)this.PartitionKey[0];

        public static string BuildPartitionKey(OpenIDProvider provider, int offset, string account)
            => $"{((int)provider).ToString()}{offset.ToString()}{account.Substring(0, 3)}";

        public static string BuildRowKey(string account)
        {
            if (account is null)
            {
                throw new ArgumentNullException(nameof(account));
            }

            return account.Trim();
        }
    }
}
