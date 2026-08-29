namespace Niobium.Messaging.ServiceBus
{
    public abstract class Constants : Niobium.Constants
    {
        public const string DefaultServiceBusFQDNSetting = "AzureWebJobsServiceBus:fullyQualifiedNamespace";
        public const string ManagedIdentitySetting = "AzureWebJobsStorage:clientId";
    }
}
