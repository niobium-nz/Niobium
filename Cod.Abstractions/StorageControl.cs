namespace Cod
{
    public class StorageControl(int permission, string resource)
    {
        public string? StartPartitionKey { get; set; }

        public string? EndPartitionKey { get; set; }

        public string? StartRowKey { get; set; }

        public string? EndRowKey { get; set; }

        public int Permission { get; } = permission;

        public string Resource { get; } = resource;
    }
}
