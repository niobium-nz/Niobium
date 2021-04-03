using Autofac;

namespace Cod.Platform.Integration.Stripe
{
    public class DependencyModule : Module
    {
        private static readonly IErrorRetriever ErrorRetriever = new InternalErrorRetriever();

        protected override void Load(ContainerBuilder builder)
        {
            InternalError.Register(ErrorRetriever);

            builder.RegisterType<StripeIntegration>();
            builder.RegisterType<StripePaymentProcessor>().AsImplementedInterfaces();
            base.Load(builder);
        }
    }
}
