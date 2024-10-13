namespace Cod.Table.StorageAccount
{
    public class StorageTableOptions
    {
        public string ConnectionString { get; set; }

        public bool EnableInteractiveIdentity { get; set; }

        public bool Validate() => !string.IsNullOrEmpty(ConnectionString);
    }
}
