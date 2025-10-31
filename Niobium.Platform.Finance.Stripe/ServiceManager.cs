using Stripe;

namespace Niobium.Platform.Finance.Stripe
{
    internal sealed class ServiceManager(PaymentServiceOptions options)
    {
        private static readonly object syncroot = new();
        private readonly Dictionary<string, Dictionary<Type, global::Stripe.Service>> services = [];

        public T GetService<T>(string tenant)
            where T : global::Stripe.Service
        {
            if (!services.ContainsKey(tenant))
            {
                lock (syncroot)
                {
                    if (!services.ContainsKey(tenant))
                    {
                        StripeConfiguration.ApiKey = options.Secrets[tenant];
                        var client = StripeConfiguration.StripeClient!;
                        StripeConfiguration.StripeClient = null;
                        services[tenant] = [];
                        services[tenant][typeof(CustomerService)] = new CustomerService(client);
                        services[tenant][typeof(ChargeService)] = new ChargeService(client);
                        services[tenant][typeof(RefundService)] = new RefundService(client);
                        services[tenant][typeof(SetupIntentService)] = new SetupIntentService(client);
                        services[tenant][typeof(PaymentMethodService)] = new PaymentMethodService(client);
                        services[tenant][typeof(PaymentIntentService)] = new PaymentIntentService(client);
                    }
                }
            }

            return (T)services[tenant][typeof(T)];
        }

    }
}
