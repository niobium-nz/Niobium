using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Cod.Platform.Entity;

namespace Cod.Platform
{
    public class HostnameDomain : PlatformDomain<Hostname>
    {
        private static readonly TimeSpan SSLCertificateLifetime = TimeSpan.FromDays(75);
        private readonly Lazy<ICertificateAuthority> certificateAuthority;
        private readonly Lazy<IEnumerable<IDNSManager>> dnsManagers;
        private readonly Lazy<IBlobRepository> blobRepository;
        private readonly Lazy<IConfigurationProvider> configuration;

        public HostnameDomain(
            Lazy<IRepository<Hostname>> repository,
            Lazy<ICertificateAuthority> certificateAuthority,
            Lazy<IEnumerable<IDNSManager>> dnsManagers,
            Lazy<IBlobRepository> blobRepository,
            Lazy<IConfigurationProvider> configuration)
            : base(repository)
        {
            this.certificateAuthority = certificateAuthority;
            this.dnsManagers = dnsManagers;
            this.blobRepository = blobRepository;
            this.configuration = configuration;
        }

        public async Task<OperationResult> UpdateSSLCertificateIfNecessaryAsync()
        {
            var entity = await this.GetEntityAsync();
            if (entity.SSLExpiry - DateTimeOffset.UtcNow > SSLCertificateLifetime)
            {
                return OperationResult.Create();
            }

            var pfxPassword = entity.Email;
            var application = await this.certificateAuthority.Value.ApplySSLCertificateAsync(
                entity.GetHostname(),
                entity.Email,
                pfxPassword,
                entity.Country,
                organization: entity.Company);

            var dnsManager = this.dnsManagers.Value.Single(m => m.Support(entity.GetHostname(), (DNSServiceProvider)entity.DNSServiceProvider));
            foreach (var requirement in application.Requirements)
            {
                var record = await dnsManager.CreateRecordAsync(requirement.Domain, requirement.Record, requirement.Type, requirement.Value);
                if (!record.IsSuccess)
                {
                    return record;
                }
            }

            var result = await application.Result();
            using (var ms = new MemoryStream(result.PFX))
            {
                await this.blobRepository.Value.PutAsync("certificates", $"{entity.GetHostname()}.pfx", ms, true);
            }

            var x509Certificate = new X509Certificate(result.PFX, pfxPassword);
            var fingerprint = x509Certificate.GetCertHashString();
            var expiry = x509Certificate.GetExpirationDateString();

            var clientID = await this.configuration.Value.GetSettingAsync<string>("SSL_CLIENT_ID");
            var clientsecret = await this.configuration.Value.GetSettingAsync<string>("SSL_CLIENT_SECRET");
            var tenantID = await this.configuration.Value.GetSettingAsync<string>("SSL_TENANT_ID");
            var subscriptionID = await this.configuration.Value.GetSettingAsync<string>("SSL_SUBSCRIPTION_ID");
            var functionResourceGroupName = await this.configuration.Value.GetSettingAsync<string>("SSL_FUNCTION_RESOURCE_GROUP");
            var functionName = await this.configuration.Value.GetSettingAsync<string>("SSL_FUNCTION_NAME");
            var azure = IAzureExtensions.CreateClient(clientID, clientsecret, tenantID, subscriptionID, true);
            var function = await azure.GetFunctionAppAsync(functionResourceGroupName, functionName);
            var hostnames = function.QueryDomainBindings();
            var firstHostname = true;
            foreach (var hostname in hostnames)
            {
                if ((entity.Wildcard && hostname.EndsWith(entity.GetHostname(), StringComparison.InvariantCultureIgnoreCase))
                    || (!entity.Wildcard && hostname.Equals(entity.GetHostname(), StringComparison.InvariantCultureIgnoreCase)))
                {
                    if (firstHostname)
                    {
                        firstHostname = false;
                        await function.UploadAndBindSSLCertificateAsync(result.PFX, pfxPassword, hostname);
                    }
                    else
                    {
                        await function.BindExistingSSLCertificateAsync(fingerprint, hostname);
                    }
                }
            }

            foreach (var requirement in application.Requirements)
            {
                var record = await dnsManager.RemoveRecordAsync(requirement.Domain, requirement.Record, requirement.Type);
                if (!record.IsSuccess)
                {
                    return record;
                }
            }

            entity.SSLExpiry = DateTimeOffset.Parse(expiry);
            await this.SaveEntityAsync();
            return OperationResult.Create();
        }
    }
}
