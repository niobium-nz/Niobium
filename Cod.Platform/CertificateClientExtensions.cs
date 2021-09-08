using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Azure.Security.KeyVault.Certificates;

namespace Cod.Platform
{
    public static class CertificateClientExtensions
    {
        private const int KEY_SIZE = 2048;

        public async static Task<X509Certificate2> IssueClientCertificateAsync(this CertificateClient certificateClient, string name, string issuer)
        {
            using var key = RSA.Create(KEY_SIZE);
            using var cert = await certificateClient.IssueClientCertificateAsync(name, issuer, key.ExportRSAPublicKey());
            var certWithKey = cert.CopyWithPrivateKey(key);
            try
            {
                certWithKey.FriendlyName = name;
            }
            catch (PlatformNotSupportedException)
            {
                // REMARK (5he11) just to make *nix happy
            }
            return certWithKey;
        }

        public async static Task<X509Certificate2> IssueClientCertificateAsync(this CertificateClient certificateClient, string name, string issuer, byte[] publicKey)
        {
            var distinguishedName = new X500DistinguishedName($"CN={name}");

            using var rsa = RSA.Create(KEY_SIZE);
            rsa.ImportRSAPublicKey(new ReadOnlySpan<byte>(publicKey), out _);
            var request = new CertificateRequest(distinguishedName, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

            request.CertificateExtensions.Add(
                new X509KeyUsageExtension(X509KeyUsageFlags.DataEncipherment | X509KeyUsageFlags.KeyEncipherment | X509KeyUsageFlags.DigitalSignature, false));

            request.CertificateExtensions.Add(
               new X509EnhancedKeyUsageExtension(
                   new OidCollection { new Oid("1.3.6.1.5.5.7.3.2") }, false));

            var ca = await certificateClient.DownloadCertificateAsync(issuer);
            using (ca.Value)
            {
                var certificate = request.Create(ca, DateTimeOffset.UtcNow, ca.Value.NotAfter.AddDays(-1), Encoding.UTF8.GetBytes(name));
                try
                {
                    certificate.FriendlyName = name;
                }
                catch (PlatformNotSupportedException)
                {
                    // REMARK (5he11) just to make *nix happy
                }
                return certificate;
            }
        }
    }
}
