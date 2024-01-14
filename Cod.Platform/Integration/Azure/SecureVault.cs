using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;

namespace Cod.Platform
{
    internal class SecureVault
    {
        public static async Task<string> GetSecretAsync(Uri keyVaultUri, string key)
        {
            if (String.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException($"'{nameof(key)}' cannot be null or whitespace.", nameof(key));
            }

            var azureServiceTokenProvider = new AzureServiceTokenProvider();
            var keyVaultClient = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(azureServiceTokenProvider.KeyVaultTokenCallback));
            var prefix = keyVaultUri.AbsoluteUri.EndsWith('/') ? keyVaultUri.AbsoluteUri[..^1] : keyVaultUri.AbsoluteUri;
            key = key.Trim().Replace('_', '-'); // Azure Key Vault does not support underscore.
            try
            {
                var secret = await keyVaultClient.GetSecretAsync($"{prefix}/secrets/{key}");
                return secret.Value;
            }
            catch (Exception)
            {
            }

            return null;
        }
    }
}
