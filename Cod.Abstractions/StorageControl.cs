namespace Cod
{
    public class StorageControl
    {
        public StorageControl(int permission, string resource)
        {
            Permission = permission;
            Resource = resource;
        }

        public string StartPartitionKey { get; set; }

        public string EndPartitionKey { get; set; }

        public string StartRowKey { get; set; }

        public string EndRowKey { get; set; }

        public int Permission { get; }

        public string Resource { get; }
    }
}
