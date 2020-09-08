using System;
using Microsoft.Azure.Cosmos.Table;

namespace Cod.Platform
{
    public class Cache : TableEntity
    {
        public string Value { get; set; }

        public bool InMemory { get; set; }

        public DateTimeOffset Expiry { get; set; }
    }
}
