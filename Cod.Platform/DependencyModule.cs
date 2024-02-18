using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Cod.Platform.Analytics;
using Cod.Platform.Authentication;
using Cod.Platform.Authorization;
using Cod.Platform.Database;
using Cod.Platform.Finance;
using Cod.Platform.Finance.WechatPay;
using Cod.Platform.Identities;
using Cod.Platform.Locking;
using Cod.Platform.Messaging;
using Cod.Platform.Notification;
using Cod.Platform.OCR.Azure;
using Cod.Platform.OCR.Baidu;
using Cod.Platform.Stoarge;
using Cod.Platform.Tenants;
using Cod.Platform.Tenants.Wechat;
using Microsoft.Extensions.DependencyInjection;

namespace Cod.Platform
{
    public static class DependencyModule
    {
        public static IServiceCollection AddCodPlatform(this IServiceCollection services)
        {
            Cod.InternalError.Register(new Cod.Platform.InternalErrorRetriever());
            services.AddTransient(typeof(Lazy<>), typeof(LazyWrapper<>));

            services.AddTransient<AzureOCRScaner>();
            services.AddTransient<AppInsights>();
            services.AddTransient<WechatIntegration>();
            services.AddTransient<BaiduIntegration>();

            services.AddTransient<ISignatureService, AzureStorageSignatureService>();
            services.AddSingleton<IIoTCommander, AzureIoTHubCommander>();
            services.AddTransient<IPaymentService, PaymentService>();
            services.AddTransient<IPaymentProcessor, WechatPaymentProcessor>();

            services.AddTransient(sp =>
            {
                string conn = ConfigurationProvider.GetSetting(Constant.TABLE_ENDPOINT);
                conn ??= ConfigurationProvider.GetSetting(Constant.STORAGE_CONNECTION_NAME);
                return new TableServiceClient(conn);
            });
            services.AddTransient(sp =>
            {
                string conn = ConfigurationProvider.GetSetting(Constant.BLOB_ENDPOINT);
                conn ??= ConfigurationProvider.GetSetting(Constant.STORAGE_CONNECTION_NAME);
                return new BlobServiceClient(conn);
            });
            services.AddTransient(sp =>
            {
                string conn = ConfigurationProvider.GetSetting(Constant.QUEUE_ENDPOINT);
                conn ??= ConfigurationProvider.GetSetting(Constant.STORAGE_CONNECTION_NAME);
                return new QueueServiceClient(conn, new QueueClientOptions { MessageEncoding = QueueMessageEncoding.Base64 });
            });

            services.AddTransient<ITableSignatureIssuer, AzureTableSignatureIssuer>();
            services.AddTransient<IBlobSignatureIssuer, AzureBlobSignatureIssuer>();
            services.AddTransient<IQueueSignatureIssuer, AzureQueueSignatureIssuer>();
            services.AddTransient<IBlobRepository, CloudBlobRepository>();

            services.AddTransient<IConfigurationProvider, ConfigurationProvider>();
            services.AddTransient<IQueue, CloudPlatformQueue>();
            services.AddTransient<ITokenBuilder, BearerTokenBuilder>();
            services.AddTransient<IStorageControl, ImpedimentControl>();
            services.AddTransient<IImpedimentPolicy, ImpedementPolicyScanProvider>();
            services.AddTransient<INotificationService, NotificationService>();
            services.AddTransient<IOpenIDManager, OpenIDManager>();
            services.AddTransient<IBusinessManager, MemoryCachedBusinessManager>();
            services.AddTransient<IBrandService, MemoryCachedBrandService>();

            services.AddTransient<WechatRepository>();
            services.AddTransient<IRepository<WechatEntity>>(sp =>
                new CachedRepository<WechatEntity>(
                    sp.GetService<WechatRepository>(),
                    sp.GetService<ICacheStore>(),
                    true
                ));

            services.AddTransient<IRepository<Cache>, CloudTableRepository<Cache>>();

            services.AddTransient<IQueryableRepository<Impediment>, CloudTableRepository<Impediment>>();
            services.AddTransient<IRepository<Impediment>, CloudTableRepository<Impediment>>();

            services.AddTransient<IQueryableRepository<Accounting>, CloudTableRepository<Accounting>>();
            services.AddTransient<IRepository<Accounting>, CloudTableRepository<Accounting>>();

            services.AddTransient<IQueryableRepository<Entitlement>, CloudTableRepository<Entitlement>>();
            services.AddTransient<IRepository<Entitlement>, CloudTableRepository<Entitlement>>();

            services.AddTransient<IQueryableRepository<Transaction>, CloudTableRepository<Transaction>>();
            services.AddTransient<IRepository<Transaction>, CloudTableRepository<Transaction>>();

            services.AddTransient<IQueryableRepository<OpenID>, CloudTableRepository<OpenID>>();
            services.AddTransient<IRepository<OpenID>, CloudTableRepository<OpenID>>();

            services.AddTransient<IQueryableRepository<Login>, CloudTableRepository<Login>>();
            services.AddTransient<IRepository<Login>, CloudTableRepository<Login>>();

            services.AddTransient<IQueryableRepository<User>, CloudTableRepository<User>>();
            services.AddTransient<IRepository<User>, CloudTableRepository<User>>();
            services.AddTransient<UserDomain>();
            services.AddTransient<Func<UserDomain>>(sp => () => sp.GetService<UserDomain>());
            services.AddTransient<IDomainRepository<UserDomain, User>, GenericDomainRepository<UserDomain, User>>();

            services.AddTransient<IQueryableRepository<Business>, CloudTableRepository<Business>>();
            services.AddTransient<IRepository<Business>, CloudTableRepository<Business>>();
            services.AddTransient<BusinessDomain>();
            services.AddTransient<Func<BusinessDomain>>(sp => () => sp.GetService<BusinessDomain>());
            services.AddTransient<IDomainRepository<BusinessDomain, Business>, GenericDomainRepository<BusinessDomain, Business>>();

            services.AddTransient<IQueryableRepository<MobileLocation>, CloudTableRepository<MobileLocation>>();
            services.AddTransient<IRepository<MobileLocation>, CloudTableRepository<MobileLocation>>();

            services.AddTransient<IQueryableRepository<BrandingInfo>, CloudTableRepository<BrandingInfo>>();
            services.AddTransient<IRepository<BrandingInfo>, CloudTableRepository<BrandingInfo>>();

            services.AddTransient<IQueryableRepository<Interest>, CloudTableRepository<Interest>>();
            services.AddTransient<IRepository<Interest>, CloudTableRepository<Interest>>();

            services.AddTransient<IQueryableRepository<Job>, CloudTableRepository<Job>>();
            services.AddTransient<IRepository<Job>, CloudTableRepository<Job>>();

            services.AddTransient<IQueryableRepository<PaymentMethod>, CloudTableRepository<PaymentMethod>>();
            services.AddTransient<IRepository<PaymentMethod>, CloudTableRepository<PaymentMethod>>();

            services.AddTransient<IQueryableRepository<Report>, CloudTableRepository<Report>>();
            services.AddTransient<IRepository<Report>, CloudTableRepository<Report>>();

            return services;
        }
    }
}