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
            base.Load(builder);
        }
    }
}
