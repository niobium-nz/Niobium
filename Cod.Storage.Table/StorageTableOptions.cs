using Microsoft.Extensions.Configuration;

namespace Cod.Storage.Table
{
    public class StorageTableOptions
    {
        public string ServiceEndpoint { get; set; }

        public bool EnableInteractiveIdentity { get; set; }

        public IConfigurationSection AzureStorageTableDefaults { get; set; }

        public bool Validate() => !string.IsNullOrEmpty(ServiceEndpoint);
    }
}
