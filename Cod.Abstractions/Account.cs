using System;

namespace Cod
{
    public class Account : IEntity
    {
        public string PartitionKey { get; set; }

        public string RowKey { get; set; }

        public DateTimeOffset Timestamp { get; set; }

        public string ETag { get; set; }

        public string Password { get; set; }

        public string Phone { get; set; }

        public bool Disabled { get; set; }

        public bool Activated { get; set; }

        public string AuthCode { get; set; }

        public string RegistrationIP { get; set; }

        public string LastIP { get; set; }

        public string Roles { get; set; }

        public DateTimeOffset AuthCodeExpiry { get; set; }

        public DateTimeOffset? Created { get; set; }

        public static string BuildPartitionKey(OpenIDProvider provider, string appID)
        {
            if (appID is null)
            {
                throw new ArgumentNullException(nameof(appID));
            }

            return $"{(int)provider}|{appID.Trim()}";
        }

        public static string BuildRowKey(string openID)
        {
            if (openID is null)
            {
                throw new ArgumentNullException(nameof(openID));
            }

            return openID.Trim();
        }

        public static string BuildFullID(OpenIDProvider provider, string appID, string openID)
        {
            if (appID is null)
            {
                throw new ArgumentNullException(nameof(appID));
            }

            if (openID is null)
            {
                throw new ArgumentNullException(nameof(openID));
            }

            return BuildFullID(BuildPartitionKey(provider, appID), BuildRowKey(openID));
        }

        public static string BuildFullID(string partitionKey, string rowKey)
        {
            if (partitionKey is null)
            {
                throw new ArgumentNullException(nameof(partitionKey));
            }

            if (rowKey is null)
            {
                throw new ArgumentNullException(nameof(rowKey));
            }

            return $"{partitionKey.Trim()}|{rowKey.Trim()}";
        }

        public static OpenIDProvider ParseProvider(string partitionKey)
        {
            if (partitionKey is null)
            {
                throw new ArgumentNullException(nameof(partitionKey));
            }

            return (OpenIDProvider)Int32.Parse(partitionKey.Trim().Split('|')[0]);
        }

        public static string ParseAppID(string partitionKey)
        {
            if (partitionKey is null)
            {
                throw new ArgumentNullException(nameof(partitionKey));
            }

            return partitionKey.Trim().Split('|')[1];
        }

        public static string ParseOpenID(string fullID)
        {
            if (fullID is null)
            {
                throw new ArgumentNullException(nameof(fullID));
            }

            return fullID.Trim().Split('|')[2];
        }

        public static void ParseFromFullID(string fullID, out OpenIDProvider provider, out string appID, out string openID)
        {
            if (fullID is null)
            {
                throw new ArgumentNullException(nameof(fullID));
            }

            var splited = fullID.Trim().Split('|');
            provider = (OpenIDProvider)Int32.Parse(splited[0]);
            appID = splited[1];
            openID = splited[2];
        }

        public string GetFullID() => BuildFullID(this.PartitionKey, this.RowKey);

        public string GetOpenID() => this.RowKey.Trim();

        public string GetAppID() => ParseAppID(this.PartitionKey);

        public OpenIDProvider GetProvider() => ParseProvider(this.PartitionKey);
    }
}
