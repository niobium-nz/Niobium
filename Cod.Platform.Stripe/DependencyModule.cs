using Autofac;

namespace Cod.Platform.Integration.Stripe
{
    public class DependencyModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<StripeIntegration>();
            builder.RegisterType<StripePaymentProcessor>().AsImplementedInterfaces();
            base.Load(builder);
        }
    }
}
