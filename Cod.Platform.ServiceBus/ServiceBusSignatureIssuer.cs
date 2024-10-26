using Cod.Platform;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Web;

namespace Cod.Messaging.ServiceBus
{
    internal class ServiceBusSignatureIssuer(IOptions<ServiceBusOptions> options) : ISignatureIssuer
    {
        public bool CanIssue(ResourceType storageType, StorageControl control)
        {
            return storageType == ResourceType.AzureServiceBus;
        }

        public Task<(string, DateTimeOffset)> IssueAsync(ResourceType storageType, StorageControl control, DateTimeOffset expires, CancellationToken cancellationToken = default)
        {
            var fdqn = control.Resource;
            var queue = control.StartPartitionKey;
            var keyName = $"{queue}-{control.Permission}";

            if (options.Value.Keys == null
                || string.IsNullOrWhiteSpace(fdqn)
                || string.IsNullOrWhiteSpace(queue)
                || control.StartPartitionKey != control.EndPartitionKey
                || !options.Value.Keys.TryGetValue(keyName, out string? key))
            {
                throw new ApplicationException(InternalError.InternalServerError);
            }

            return Task.FromResult((CreateToken(fdqn, queue, keyName, key, expires), expires));
        }

        private static string CreateToken(string fdqn, string queue, string keyName, string key, DateTimeOffset expiry)
        {
            var expiryEpoch = expiry.ToUnixTimeSeconds();
            var resourceUri = $"{fdqn}/{queue}";
            string stringToSign = $"{HttpUtility.UrlEncode(resourceUri)}\n{expiryEpoch}";
            var hash = SHA.SHA256HashBytes(stringToSign, key);
            var signature = Convert.ToBase64String(hash);
            return String.Format(CultureInfo.InvariantCulture, "sr={0}&sig={1}&se={2}&skn={3}", HttpUtility.UrlEncode(resourceUri), HttpUtility.UrlEncode(signature), expiryEpoch, keyName);
        }
    }
}
