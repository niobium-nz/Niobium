namespace Niobium.Database.StorageTable
{
    public abstract class Constants : Niobium.Constants
    {
        public const string AzureTableETagKey = "odata.etag";
        public const string DefaultTableServiceUriSetting = "AzureWebJobsStorage:tableServiceUri";
        public const string ManagedIdentitySetting = "AzureWebJobsStorage:clientId";
    }
}
