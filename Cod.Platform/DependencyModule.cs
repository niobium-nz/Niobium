using Autofac;
using Cod.Platform.Model;
using Microsoft.Extensions.Logging;

namespace Cod.Platform
{
    public class DependencyModule : Module
    {
        private readonly string wechatReverseProxy;
        private readonly string wechatPayReverseProxy;

        public DependencyModule(string wechatReverseProxy, string wechatPayReverseProxy)
        {
            this.wechatReverseProxy = wechatReverseProxy;
            this.wechatPayReverseProxy = wechatPayReverseProxy;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.Register(_ => Logger.Instance).As<ILogger>();
            WechatHelper.Initialize(this.wechatReverseProxy, this.wechatPayReverseProxy);

            builder.RegisterType<CloudTableRepository<Model.Account>>().As<IRepository<Model.Account>>();
            builder.RegisterType<CloudTableRepository<Model.Accounting>>().As<IRepository<Model.Accounting>>();
            builder.RegisterType<CloudTableRepository<Model.Entitlement>>().As<IRepository<Model.Entitlement>>();
            builder.RegisterType<CloudTableRepository<Model.Transaction>>().As<IRepository<Model.Transaction>>();

            builder.RegisterType<ChargeRepository>().As<IRepository<Charge>>();
            builder.RegisterType<WechatRepository>().AsSelf();
            builder.Register(ctx => new CachedRepository<WechatEntity>(
                    ctx.Resolve<WechatRepository>(),
                    ctx.Resolve<ICacheStore>(),
                    true
                )).As<IRepository<WechatEntity>>();
            builder.RegisterType<CloudSignatureIssuer>().AsImplementedInterfaces();
            builder.RegisterType<ConfigurationProvider>().AsImplementedInterfaces();
            builder.RegisterType<QueueMessageRepository>().AsImplementedInterfaces();
            builder.RegisterType<BrandingRepository>().AsImplementedInterfaces();
            builder.RegisterType<TokenValidator>().AsImplementedInterfaces();
            builder.RegisterType<CloudTableRepository<Impediment>>().AsImplementedInterfaces();
            builder.RegisterType<ImpedimentControl>().AsImplementedInterfaces();
            builder.RegisterType<ImpedementPolicyScanProvider>().AsImplementedInterfaces();
            builder.RegisterType<CloudBlobRepository>().AsImplementedInterfaces();
            base.Load(builder);
        }
    }
}
