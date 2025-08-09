using Azure;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

namespace Cod.Platform
{
    internal class SecureVault
    {
        public static async Task<string?> GetSecretAsync(Uri keyVaultUri, string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException($"'{nameof(key)}' cannot be null or whitespace.", nameof(key));
            }

            SecretClient client = new(vaultUri: keyVaultUri, credential: new DefaultAzureCredential());
            string prefix = keyVaultUri.AbsoluteUri.EndsWith('/') ? keyVaultUri.AbsoluteUri[..^1] : keyVaultUri.AbsoluteUri;
            key = key.Trim().Replace('_', '-'); // Azure Key Vault does not support underscore.
            try
            {
                Response<KeyVaultSecret> secret = await client.GetSecretAsync($"{prefix}/secrets/{key}");
                return secret.Value.Value;
            }
            catch (Exception)
            {
            }

            return null;
        }
    }
}
