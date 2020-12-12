using Autofac;
using Microsoft.Extensions.Logging;

namespace Cod.Platform
{
    public class DependencyModule : Module
    {
        private static readonly IErrorRetriever ErrorRetriever = new InternalErrorRetriever();

        protected override void Load(ContainerBuilder builder)
        {
            InternalError.Register(ErrorRetriever);
            builder.Register(_ => Logger.Instance).As<ILogger>();
            builder.RegisterType<AzureOCRScaner>();
            builder.RegisterType<AzureStorageSignatureService>().AsImplementedInterfaces();
            builder.RegisterType<WechatIntegration>();
            builder.RegisterType<BaiduIntegration>();
            builder.RegisterType<WindcaveIntegration>();
            builder.RegisterType<CloudTableRepository<Account>>().As<IRepository<Account>>();
            builder.RegisterType<CloudTableRepository<Accounting>>().As<IRepository<Accounting>>();
            builder.RegisterType<CloudTableRepository<Entitlement>>().As<IRepository<Entitlement>>();
            builder.RegisterType<CloudTableRepository<Transaction>>().As<IRepository<Transaction>>();
            builder.RegisterType<CloudTableRepository<OpenID>>().AsImplementedInterfaces();
            builder.RegisterType<CloudTableRepository<Login>>().AsImplementedInterfaces();
            builder.RegisterType<CloudTableRepository<User>>().AsImplementedInterfaces();
            builder.RegisterType<CloudTableRepository<Business>>().AsImplementedInterfaces();
            builder.RegisterType<CloudTableRepository<MobileLocation>>().AsImplementedInterfaces();
            builder.RegisterType<CloudTableRepository<BrandingInfo>>().AsImplementedInterfaces();
            builder.RegisterType<CloudTableRepository<Interest>>().AsImplementedInterfaces();
            builder.RegisterType<CloudTableRepository<Job>>().AsImplementedInterfaces();
            builder.RegisterType<CloudTableRepository<Hostname>>().AsImplementedInterfaces();
            builder.RegisterType<CloudTableRepository<PaymentMethod>>().AsImplementedInterfaces();

            builder.RegisterType<UserDomain>();
            builder.RegisterType<GenericDomainRepository<UserDomain, User>>().As<IDomainRepository<UserDomain, User>>();

            builder.RegisterType<HostnameDomain>();
            builder.RegisterType<GenericDomainRepository<HostnameDomain, Hostname>>().As<IDomainRepository<HostnameDomain, Hostname>>();

            builder.RegisterType<BusinessDomain>();
            builder.RegisterType<GenericDomainRepository<BusinessDomain, Business>>().As<IDomainRepository<BusinessDomain, Business>>();

            builder.RegisterType<PaymentService>().AsImplementedInterfaces();
            builder.RegisterType<WindcavePaymentProcessor>();
            builder.Register(context => context.Resolve<WindcavePaymentProcessor>()).As<IPaymentProcessor>();
            builder.RegisterType<WechatPaymentProcessor>().AsImplementedInterfaces();

            builder.RegisterType<WechatRepository>().AsSelf();
            builder.Register(ctx => new CachedRepository<WechatEntity>(
                    ctx.Resolve<WechatRepository>(),
                    ctx.Resolve<ICacheStore>(),
                    true
                )).As<IRepository<WechatEntity>>();
            builder.RegisterType<CloudSignatureIssuer>().AsImplementedInterfaces();
            builder.RegisterType<ConfigurationProvider>().AsImplementedInterfaces();
            builder.RegisterType<PlatformQueue>().AsImplementedInterfaces();
            builder.RegisterType<BearerTokenBuilder>().AsImplementedInterfaces();
            builder.RegisterType<CloudTableRepository<Impediment>>().AsImplementedInterfaces();
            builder.RegisterType<ImpedimentControl>().AsImplementedInterfaces();
            builder.RegisterType<ImpedementPolicyScanProvider>().AsImplementedInterfaces();
            builder.RegisterType<CloudBlobRepository>().AsImplementedInterfaces();
            builder.RegisterType<NotificationService>().AsImplementedInterfaces();
            builder.RegisterType<OpenIDManager>().AsImplementedInterfaces();
            builder.RegisterType<MemoryCachedBusinessManager>().AsImplementedInterfaces();
            builder.RegisterType<MemoryCachedBrandService>().AsImplementedInterfaces();
            builder.RegisterType<LetsEncryptClient>().AsImplementedInterfaces();
            builder.RegisterType<AliyunDNSHelper>().AsImplementedInterfaces();
            base.Load(builder);
        }
    }
}