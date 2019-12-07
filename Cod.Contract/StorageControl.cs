namespace Cod.Contract
{
    public class StorageControl
    {
        public StorageControl(int permission, string resource)
        {
            this.Permission = permission;
            this.Resource = resource;
        }

        public string StartPartitionKey { get; set; }

        public string EndPartitionKey { get; set; }

        public string StartRowKey { get; set; }

        public string EndRowKey { get; set; }

        public int Permission { get; }

        public string Resource { get; }
    }
}
