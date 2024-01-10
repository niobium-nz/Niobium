using Autofac;
using Microsoft.Extensions.Logging;

namespace Cod.Platform
{
    public class DependencyModule : Module
    {
        private static readonly IErrorRetriever ErrorRetriever = new InternalErrorRetriever();

        protected override void Load(ContainerBuilder builder)
        {
            //builder.RegisterModule<LoggerModule>();

            InternalError.Register(ErrorRetriever);
            builder.Register(_ => Logger.Instance).As<ILogger>();
            builder.RegisterType<AzureOCRScaner>();
            builder.RegisterType<AppInsights>();
            builder.RegisterType<AzureStorageSignatureService>().AsImplementedInterfaces();
            builder.RegisterType<AzureIoTHubCommander>().AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<WechatIntegration>();
            builder.RegisterType<BaiduIntegration>();
            builder.RegisterType<CloudTableRepository<Account>>().AsImplementedInterfaces();
            builder.RegisterType<CloudTableRepository<Accounting>>().AsImplementedInterfaces();
            builder.RegisterType<CloudTableRepository<Entitlement>>().AsImplementedInterfaces();
            builder.RegisterType<CloudTableRepository<Transaction>>().AsImplementedInterfaces();
            builder.RegisterType<CloudTableRepository<OpenID>>().AsImplementedInterfaces();
            builder.RegisterType<CloudTableRepository<Login>>().AsImplementedInterfaces();
            builder.RegisterType<CloudTableRepository<User>>().AsImplementedInterfaces();
            builder.RegisterType<CloudTableRepository<Business>>().AsImplementedInterfaces();
            builder.RegisterType<CloudTableRepository<MobileLocation>>().AsImplementedInterfaces();
            builder.RegisterType<CloudTableRepository<BrandingInfo>>().AsImplementedInterfaces();
            builder.RegisterType<CloudTableRepository<Interest>>().AsImplementedInterfaces();
            builder.RegisterType<CloudTableRepository<Job>>().AsImplementedInterfaces();
            builder.RegisterType<CloudTableRepository<PaymentMethod>>().AsImplementedInterfaces();
            builder.RegisterType<CloudTableRepository<Report>>().AsImplementedInterfaces();

            builder.RegisterType<UserDomain>();
            builder.RegisterType<GenericDomainRepository<UserDomain, User>>().As<IDomainRepository<UserDomain, User>>();

            builder.RegisterType<BusinessDomain>();
            builder.RegisterType<GenericDomainRepository<BusinessDomain, Business>>().As<IDomainRepository<BusinessDomain, Business>>();

            builder.RegisterType<PaymentService>().AsImplementedInterfaces();
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
            base.Load(builder);
        }
    }
}