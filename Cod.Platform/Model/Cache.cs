using System;
using Microsoft.WindowsAzure.Storage.Table;

namespace Cod.Platform.Model
{
    public class Cache : TableEntity
    {
        public string Value { get; set; }

        public bool InMemory { get; set; }

        public DateTimeOffset Expiry { get; set; }
    }
}
