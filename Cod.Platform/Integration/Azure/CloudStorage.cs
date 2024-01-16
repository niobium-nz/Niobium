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
        {
            var conn = ConfigurationProvider.GetSetting(Constant.BLOB_ENDPOINT);
            conn ??= ConfigurationProvider.GetSetting(Constant.STORAGE_CONNECTION_NAME);
            return GetStorageAccount(conn);
        }

        public static CloudTable GetTable<T>(string connectionString) where T : ITableEntity
            => GetTable(typeof(T).Name, connectionString);

        public static CloudTable GetTable<T>() where T : ITableEntity
            => GetTable(typeof(T).Name);

        public static CloudTable GetTable(string tableName, string connectionString)
            => GetStorageAccount(connectionString).CreateCloudTableClient().GetTableReference(tableName);

        public static CloudTable GetTable(string tableName)
        {
            var conn = ConfigurationProvider.GetSetting(Constant.TABLE_ENDPOINT);
            conn ??= ConfigurationProvider.GetSetting(Constant.STORAGE_CONNECTION_NAME);
            return GetStorageAccount(conn).CreateCloudTableClient().GetTableReference(tableName);
        }

        public static CloudQueue GetQueue(string queueName)
        {
            var conn = ConfigurationProvider.GetSetting(Constant.QUEUE_ENDPOINT);
            conn ??= ConfigurationProvider.GetSetting(Constant.STORAGE_CONNECTION_NAME);
            return GetStorageAccount(conn).CreateCloudQueueClient().GetQueueReference(queueName.Trim().ToLowerInvariant());
        }

        public static CloudBlobContainer GetBlobContainer(string containerName)
        {
            var conn = ConfigurationProvider.GetSetting(Constant.BLOB_ENDPOINT);
            conn ??= ConfigurationProvider.GetSetting(Constant.STORAGE_CONNECTION_NAME);
            return GetStorageAccount(conn).CreateCloudBlobClient().GetContainerReference(containerName);
        }

        public static CloudBlockBlob GetBlob(string containerName, string blobName)
        {
            var conn = ConfigurationProvider.GetSetting(Constant.BLOB_ENDPOINT);
            conn ??= ConfigurationProvider.GetSetting(Constant.STORAGE_CONNECTION_NAME);
            return GetStorageAccount(conn).CreateCloudBlobClient().GetContainerReference(containerName).GetBlockBlobReference(blobName);
        }
    }
}
