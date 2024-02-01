using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace Cod.Platform.Integration.Azure
{
    internal static class CloudStorage
    {
        public static CloudStorageAccount GetStorageAccount(string connectionString)
        {
            return CloudStorageAccount.Parse(connectionString);
        }

        public static CloudStorageAccount GetStorageAccount()
        {
            string conn = ConfigurationProvider.GetSetting(Constant.BLOB_ENDPOINT);
            conn ??= ConfigurationProvider.GetSetting(Constant.STORAGE_CONNECTION_NAME);
            return GetStorageAccount(conn);
        }

        public static CloudTable GetTable<T>(string connectionString) where T : ITableEntity
        {
            return GetTable(typeof(T).Name, connectionString);
        }

        public static CloudTable GetTable<T>() where T : ITableEntity
        {
            return GetTable(typeof(T).Name);
        }

        public static CloudTable GetTable(string tableName, string connectionString)
        {
            return GetStorageAccount(connectionString).CreateCloudTableClient().GetTableReference(tableName);
        }

        public static CloudTable GetTable(string tableName)
        {
            string conn = ConfigurationProvider.GetSetting(Constant.TABLE_ENDPOINT);
            conn ??= ConfigurationProvider.GetSetting(Constant.STORAGE_CONNECTION_NAME);
            return GetStorageAccount(conn).CreateCloudTableClient().GetTableReference(tableName);
        }
    }
}
