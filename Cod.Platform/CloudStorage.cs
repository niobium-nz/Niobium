using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;

namespace Cod.Platform
{
    static class CloudStorage
    {
        public static CloudStorageAccount GetAccount(string connectionString)
            => CloudStorageAccount.Parse(connectionString);

        public static CloudStorageAccount GetAccount()
            => GetAccount(ConfigurationProvider.GetSetting(Constant.STORAGE_CONNECTION_NAME));

        public static CloudTable GetTable<T>(string connectionString) where T : ITableEntity
            => GetTable(typeof(T).Name, connectionString);

        public static CloudTable GetTable<T>() where T : ITableEntity
            => GetTable(typeof(T).Name);

        public static CloudTable GetTable(string tableName, string connectionString)
            => GetAccount(connectionString).CreateCloudTableClient().GetTableReference(tableName);

        public static CloudTable GetTable(string tableName)
            => GetAccount().CreateCloudTableClient().GetTableReference(tableName);

        public static CloudQueue GetQueue(string queueName)
            => GetAccount().CreateCloudQueueClient().GetQueueReference(queueName.Trim().ToLowerInvariant());

        public static CloudBlobContainer GetBlobContainer(string containerName)
            => GetAccount().CreateCloudBlobClient().GetContainerReference(containerName);

        public static CloudBlockBlob GetBlob(string containerName, string blobName)
            => GetAccount().CreateCloudBlobClient().GetContainerReference(containerName).GetBlockBlobReference(blobName);
    }
}
