using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Azure.Security.KeyVault.Certificates;

namespace Cod.Platform
{
    public static class X509CertificateIssuer
    {
        public async static Task<X509Certificate2> RetrieveOrIssueClientCertificateAsync(this CertificateClient client, string name)
        {
            try
            {
                var cert = await client.DownloadCertificateAsync(name);
                return cert;
            }
            catch (Azure.RequestFailedException e)
            {
                if (e.Status == 404)
                {
                    string pwd;
                    using (RandomNumberGenerator rng = new RNGCryptoServiceProvider())
                    {
                        byte[] tokenData = new byte[32];
                        rng.GetBytes(tokenData);

                        pwd = Convert.ToBase64String(tokenData);
                    }

                    var cert = await client.IssueClientCertificateAsync(name);
                    var buff = cert.Export(X509ContentType.Pfx, pwd);
                    await client.ImportCertificateAsync(new ImportCertificateOptions(name, buff)
                    {
                        Password = pwd,
                        Enabled = true,
                    });
                    return cert;
                }

                throw;
            }
        }

        public async static Task<X509Certificate2> IssueClientCertificateAsync(this CertificateClient client, string name)
        {
            var distinguishedName = new X500DistinguishedName($"CN={name}");

            using var rsa = RSA.Create(2048);
            var request = new CertificateRequest(distinguishedName, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

            request.CertificateExtensions.Add(
                new X509KeyUsageExtension(X509KeyUsageFlags.DataEncipherment | X509KeyUsageFlags.KeyEncipherment | X509KeyUsageFlags.DigitalSignature, false));

            request.CertificateExtensions.Add(
               new X509EnhancedKeyUsageExtension(
                   new OidCollection { new Oid("1.3.6.1.5.5.7.3.2") }, false));

            var ca = await client.DownloadCertificateAsync("nest-intermediate-primary");
            using var certificate = request.Create(ca, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddYears(1), Encoding.UTF8.GetBytes(name));
            certificate.FriendlyName = name;
            var result = certificate.CopyWithPrivateKey(rsa);
            return result;
        }
    }
}
