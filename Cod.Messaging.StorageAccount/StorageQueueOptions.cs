namespace Cod.Messaging.StorageAccount
{
    public class StorageQueueOptions
    {
        public required string ServiceEndpoint { get; set; }

        public bool EnableInteractiveIdentity { get; set; }

        public bool Base64MessageEncoding { get; set; }

        public bool CreateQueueIfNotExist { get; set; } = true;

        public bool Validate()
        {
            return !string.IsNullOrEmpty(ServiceEndpoint);
        }
    }
}
