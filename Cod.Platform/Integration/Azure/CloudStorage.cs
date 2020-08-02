using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.Storage.Queue;

namespace Cod.Platform
{
    internal static class CloudStorage
    {
        public static Microsoft.Azure.Storage.CloudStorageAccount GetStorageAccount(string connectionString)
            => Microsoft.Azure.Storage.CloudStorageAccount.Parse(connectionString);

        public static Microsoft.Azure.Storage.CloudStorageAccount GetStorageAccount()
            => GetStorageAccount(ConfigurationProvider.GetSetting(Constant.STORAGE_CONNECTION_NAME));

        public static CloudStorageAccount GetTableAccount(string connectionString)
            => CloudStorageAccount.Parse(connectionString);

        public static CloudStorageAccount GetTableAccount()
            => GetTableAccount(ConfigurationProvider.GetSetting(Constant.STORAGE_CONNECTION_NAME));

        public static CloudTable GetTable<T>(string connectionString) where T : ITableEntity
            => GetTable(typeof(T).Name, connectionString);

        public static CloudTable GetTable<T>() where T : ITableEntity
            => GetTable(typeof(T).Name);

        public static CloudTable GetTable(string tableName, string connectionString)
            => GetTableAccount(connectionString).CreateCloudTableClient().GetTableReference(tableName);

        public static CloudTable GetTable(string tableName)
            => GetTableAccount().CreateCloudTableClient().GetTableReference(tableName);

        public static CloudQueue GetQueue(string queueName)
            => GetStorageAccount().CreateCloudQueueClient().GetQueueReference(queueName.Trim().ToLowerInvariant());

        public static CloudBlobContainer GetBlobContainer(string containerName)
            => GetStorageAccount().CreateCloudBlobClient().GetContainerReference(containerName);

        public static CloudBlockBlob GetBlob(string containerName, string blobName)
            => GetStorageAccount().CreateCloudBlobClient().GetContainerReference(containerName).GetBlockBlobReference(blobName);
    }
}
