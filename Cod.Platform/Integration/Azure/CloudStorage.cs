using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;

namespace Cod.Platform
{
    internal static class CloudStorage
    {
        public static CloudStorageAccount GetStorageAccount(string connectionString)
            => CloudStorageAccount.Parse(connectionString);

        public static CloudStorageAccount GetStorageAccount()
            => GetStorageAccount(ConfigurationProvider.GetSetting(Constant.STORAGE_CONNECTION_NAME));

        public static CloudTable GetTable<T>(string connectionString) where T : ITableEntity
            => GetTable(typeof(T).Name, connectionString);

        public static CloudTable GetTable<T>() where T : ITableEntity
            => GetTable(typeof(T).Name);

        public static CloudTable GetTable(string tableName, string connectionString)
            => GetStorageAccount(connectionString).CreateCloudTableClient().GetTableReference(tableName);

        public static CloudTable GetTable(string tableName)
            => GetStorageAccount().CreateCloudTableClient().GetTableReference(tableName);

        public static CloudQueue GetQueue(string queueName)
            => GetStorageAccount().CreateCloudQueueClient().GetQueueReference(queueName.Trim().ToLowerInvariant());

        public static CloudBlobContainer GetBlobContainer(string containerName)
            => GetStorageAccount().CreateCloudBlobClient().GetContainerReference(containerName);

        public static CloudBlockBlob GetBlob(string containerName, string blobName)
            => GetStorageAccount().CreateCloudBlobClient().GetContainerReference(containerName).GetBlockBlobReference(blobName);
    }
}
