using Autofac;
using Cod.Platform.Model;
using Microsoft.Extensions.Logging;

namespace Cod.Platform
{
    public class DependencyModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.Register(_ => Logger.Instance).As<ILogger>();
            builder.RegisterType<WechatIntegration>();
            builder.RegisterType<BaiduIntegration>();
            builder.RegisterType<CloudTableRepository<Model.Account>>().As<IRepository<Model.Account>>();
            builder.RegisterType<CloudTableRepository<Model.Accounting>>().As<IRepository<Model.Accounting>>();
            builder.RegisterType<CloudTableRepository<Model.Entitlement>>().As<IRepository<Model.Entitlement>>();
            builder.RegisterType<CloudTableRepository<Model.Transaction>>().As<IRepository<Model.Transaction>>();
            builder.RegisterType<CloudTableRepository<Model.OpenID>>().AsImplementedInterfaces();
            builder.RegisterType<CloudTableRepository<Model.Login>>().AsImplementedInterfaces();
            builder.RegisterType<CloudTableRepository<Model.User>>().AsImplementedInterfaces();
            builder.RegisterType<CloudTableRepository<Model.Business>>().AsImplementedInterfaces();
            builder.RegisterType<CloudTableRepository<Model.MobileLocation>>().AsImplementedInterfaces();

            builder.RegisterType<UserDomain>();
            builder.RegisterType<GenericDomainRepository<UserDomain, Model.User>>().As<IDomainRepository<UserDomain, Model.User>>();

            builder.RegisterType<BusinessDomain>();
            builder.RegisterType<GenericDomainRepository<BusinessDomain, Model.Business>>().As<IDomainRepository<BusinessDomain, Model.Business>>();

            builder.RegisterType<ChargeRepository>().As<IRepository<Charge>>();
            builder.RegisterType<WechatRepository>().AsSelf();
            builder.Register(ctx => new CachedRepository<WechatEntity>(
                    ctx.Resolve<WechatRepository>(),
                    ctx.Resolve<ICacheStore>(),
                    true
                )).As<IRepository<WechatEntity>>();
            builder.RegisterType<CloudSignatureIssuer>().AsImplementedInterfaces();
            builder.RegisterType<ConfigurationProvider>().AsImplementedInterfaces();
            builder.RegisterType<PlatformQueue>().AsImplementedInterfaces();
            builder.RegisterType<BrandingRepository>().AsImplementedInterfaces();
            builder.RegisterType<TokenValidator>().AsImplementedInterfaces();
            builder.RegisterType<CloudTableRepository<Impediment>>().AsImplementedInterfaces();
            builder.RegisterType<ImpedimentControl>().AsImplementedInterfaces();
            builder.RegisterType<ImpedementPolicyScanProvider>().AsImplementedInterfaces();
            builder.RegisterType<CloudBlobRepository>().AsImplementedInterfaces();
            builder.RegisterType<NotificationService>().AsImplementedInterfaces();
            builder.RegisterType<OpenIDManager>().AsImplementedInterfaces();
            base.Load(builder);
        }
    }
}
