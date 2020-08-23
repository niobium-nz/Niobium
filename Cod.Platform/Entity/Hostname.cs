using System;
using Microsoft.Azure.Cosmos.Table;

namespace Cod.Platform.Entity
{
    public class Hostname : TableEntity, IEntity
    {
        public DateTimeOffset? Created { get; set; }
        
        public bool Wildcard { get; set; }

        public DateTimeOffset SSLExpiry { get; set; }

        public int DNSServiceProvider { get; set; }

        public string Country { get; set; }

        public string Company { get; set; }

        public string Email { get; set; }

        public string GetHostname() => this.RowKey;

        public static string BuildPartitionKey() => "dummy";

        public static string BuildRowKey(string hostname) => hostname.Trim().ToLowerInvariant();
    }
}
