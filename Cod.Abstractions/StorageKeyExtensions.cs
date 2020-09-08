using System;

namespace Cod
{
    public static class StorageKeyExtensions
    {
        public static string BuildFullID(this StorageKey key)
        {
            return $"{key.PartitionKey}$$${key.RowKey}";
        }

        public static StorageKey ParseFullID(string fullID)
        {
            var splited = fullID.Split(new string[] { "$$$" }, System.StringSplitOptions.RemoveEmptyEntries);
            if (splited.Length == 2)
            {
                return new StorageKey { PartitionKey = splited[0], RowKey = splited[1] };
            }
            throw new NotSupportedException();
        }
    }
}
