using Microsoft.Azure.Management.AppService.Fluent;
using Microsoft.Azure.Management.AppService.Fluent.Models;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;

namespace Cod.Platform
{
    internal static class IAzureExtensions
    {
        public static IAzure CreateClient(string clientID, string clientSecret, string tenantID, string subscriptionID, bool isAzureChina)
        {
            var credentials = SdkContext.AzureCredentialsFactory.FromServicePrincipal(
                clientID,
                clientSecret,
                tenantID,
                isAzureChina ? AzureEnvironment.AzureChinaCloud : AzureEnvironment.AzureGlobalCloud);

            return Microsoft.Azure.Management.Fluent.Azure.Configure()
                .WithLogLevel(HttpLoggingDelegatingHandler.Level.Basic)
                .Authenticate(credentials)
                .WithSubscription(subscriptionID);
        }

        public static async Task<IFunctionApp> GetFunctionAppAsync(this IAzure azure, string resourceGroup, string name) => await azure.AppServices.FunctionApps.GetByResourceGroupAsync(resourceGroup, name);

        public static async Task<IFunctionApp> GetFunctionAppAsync(this IAzure azure, string funcId) => await azure.AppServices.FunctionApps.GetByIdAsync(funcId);

        public static IEnumerable<string> QueryDomainBindings(this IFunctionApp function) => function.EnabledHostNames;

        public static async Task AddHostnameBindingAsync(this IFunctionApp function, string domain, string subDomain)
            => await function.Update()
            .DefineHostnameBinding()
            .WithThirdPartyDomain(domain)
            .WithSubDomain(subDomain)
            .WithDnsRecordType(CustomHostNameDnsRecordType.CName)
            .Attach().ApplyAsync();

        public static async Task RemoveHostnameBindingAsync(this IFunctionApp function, string hostname) => await function.Update().WithoutHostnameBinding(hostname).ApplyAsync();

        public static async Task UploadAndBindSSLCertificateAsync(this IFunctionApp functionApp, byte[] pfx, string password, string hostname)
            => await functionApp.Update()
                .WithThirdPartyHostnameBinding(hostname)
                .DefineSslBinding()
                .ForHostname(hostname)
                .WithPfxByteArrayToUpload(pfx, password)
                .WithSniBasedSsl()
                .Attach()
                .ApplyAsync();

        public static async Task BindExistingSSLCertificateAsync(this IFunctionApp functionApp, string certificateNameOrThumbprint, string hostname)
            => await functionApp.Update()
                .WithThirdPartyHostnameBinding(hostname)
                .DefineSslBinding()
                .ForHostname(hostname)
                .WithExistingCertificate(certificateNameOrThumbprint)
                .WithSniBasedSsl()
                .Attach()
                .ApplyAsync();

        public static async Task RemoveSSLCertificateBindingAsync(this IFunctionApp functionApp, string hostname) => await functionApp.Update().WithoutSslBinding(hostname).ApplyAsync();
    }
}
