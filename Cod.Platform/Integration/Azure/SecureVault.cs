using System.Threading.Tasks;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;

namespace Cod.Platform
{
    internal class SecureVault
    {
        public static async Task<string> GetSecretAsync(string key)
        {
            var azureServiceTokenProvider = new AzureServiceTokenProvider();
            var keyVaultClient = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(azureServiceTokenProvider.KeyVaultTokenCallback));
            var secret = await keyVaultClient.GetSecretAsync($"{Constant.KEY_VAULT_URL}/secrets/{key}");
            return secret.Value;
        }
    }
}
