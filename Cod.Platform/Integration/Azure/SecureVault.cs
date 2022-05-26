using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;

namespace Cod.Platform
{
    internal class SecureVault
    {
        public static async Task<string> GetSecretAsync(Uri keyVaultUri, string key)
        {
            var azureServiceTokenProvider = new AzureServiceTokenProvider();
            var keyVaultClient = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(azureServiceTokenProvider.KeyVaultTokenCallback));
            var secret = await keyVaultClient.GetSecretAsync($"{keyVaultUri.AbsoluteUri}/secrets/{key}");
            return secret.Value;
        }
    }
}
