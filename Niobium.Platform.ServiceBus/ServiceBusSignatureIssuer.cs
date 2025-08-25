using Microsoft.Extensions.Options;
using Niobium.Platform;
using System.Globalization;
using System.Web;

namespace Niobium.Messaging.ServiceBus
{
    internal sealed class ServiceBusSignatureIssuer(IOptions<ServiceBusOptions> options) : ISignatureIssuer
    {
        public bool CanIssue(ResourceType storageType, StorageControl control)
        {
            return storageType == ResourceType.AzureServiceBus;
        }

        public Task<(string, DateTimeOffset)> IssueAsync(ResourceType storageType, StorageControl control, DateTimeOffset expires, CancellationToken cancellationToken = default)
        {
            string fdqn = control.Resource;
            string? queue = control.StartPartitionKey;
            string keyName = $"{queue}-{control.Permission}";

            return options.Value.Keys == null
                || string.IsNullOrWhiteSpace(fdqn)
                || string.IsNullOrWhiteSpace(queue)
                || control.StartPartitionKey != control.EndPartitionKey
                || !options.Value.Keys.TryGetValue(keyName, out string? key)
                ? throw new ApplicationException(InternalError.InternalServerError)
                : (Task<(string, DateTimeOffset)>)Task.FromResult((CreateToken(fdqn, queue, keyName, key, expires), expires));
        }

        private static string CreateToken(string fdqn, string queue, string keyName, string key, DateTimeOffset expiry)
        {
            long expiryEpoch = expiry.ToUnixTimeSeconds();
            string resourceUri = $"{fdqn}/{queue}";
            string stringToSign = $"{HttpUtility.UrlEncode(resourceUri)}\n{expiryEpoch}";
            byte[] hash = SHA.SHA256HashBytes(stringToSign, key);
            string signature = Convert.ToBase64String(hash);
            return string.Format(CultureInfo.InvariantCulture, "sr={0}&sig={1}&se={2}&skn={3}", HttpUtility.UrlEncode(resourceUri), HttpUtility.UrlEncode(signature), expiryEpoch, keyName);
        }
    }
}
