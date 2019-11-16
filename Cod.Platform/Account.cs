using System;
using Cod.Contract;
using Microsoft.WindowsAzure.Storage.Table;

namespace Cod.Platform
{
    public class Account : TableEntity, IImpedable
    {
        public string Password { get; set; }

        public string Phone { get; set; }

        public bool Disabled { get; set; }

        public bool Activated { get; set; }

        public string AuthCode { get; set; }

        public string RegistrationIP { get; set; }

        public string LastIP { get; set; }

        public string Roles { get; set; }

        public bool Impeded { get; set; }

        public DateTimeOffset AuthCodeExpiry { get; set; }

        public string GetFullID() => BuildFullID(this.PartitionKey, this.RowKey);

        public string GetOpenID() => this.RowKey.Trim();

        public string GetAppID() => ParseAppID(this.PartitionKey);

        public OpenIDProvider GetProvider() => ParseProvider(this.PartitionKey);

        public static string BuildPartitionKey(OpenIDProvider provider, string appID) => $"{(int)provider}|{appID.Trim()}";

        public static string BuildRowKey(string openID) => openID.Trim();

        public static string BuildFullID(string partitionKey, string rowKey) => $"{partitionKey.Trim()}|{rowKey.Trim()}";

        public static OpenIDProvider ParseProvider(string partitionKey) => (OpenIDProvider)Int32.Parse(partitionKey.Trim().Split('|')[0]);

        public static string ParseAppID(string partitionKey) => partitionKey.Trim().Split('|')[1];

        public static void ParseFromFullID(string fullID, out OpenIDProvider provider, out string appID, out string openID)
        {
            var splited = fullID.Trim().Split('|');
            provider = (OpenIDProvider)Int32.Parse(splited[0]);
            appID = splited[1];
            openID = splited[2];
        }

        public string GetImpedementID() => $"{nameof(Account).ToUpperInvariant()}-{this.GetOpenID()}";
    }
}
