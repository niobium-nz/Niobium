namespace Niobium
{
    public static class StorageKeyExtensions
    {
        public static string BuildFullID(this StorageKey key)
        {
            return $"{key.PartitionKey}%{key.RowKey}";
        }

        public static StorageKey ParseFullID(string fullID)
        {
            string[] splited = fullID.Split(["%"], StringSplitOptions.RemoveEmptyEntries);
            return splited.Length == 2 ? new StorageKey { PartitionKey = splited[0], RowKey = splited[1] } : throw new NotSupportedException();
        }
    }
}
