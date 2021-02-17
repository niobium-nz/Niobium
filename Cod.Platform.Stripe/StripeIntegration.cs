using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Sockets;
using System.Security.Claims;
using System.Threading.Tasks;
using Stripe;

namespace Cod.Platform
{
    public class StripeIntegration
    {
        private readonly Lazy<IConfigurationProvider> configuration;

        public StripeIntegration(Lazy<IConfigurationProvider> configuration) => this.configuration = configuration;

        public async Task<OperationResult<SetupIntent>> CreateSetupIntentAsync(Guid user)
        {
            StripeConfiguration.ApiKey = await this.configuration.Value.GetSettingAsync<string>("STRIPE_KEY");
            var options = new CustomerCreateOptions();
            var service = new CustomerService();
            var customer = await service.CreateAsync(options);

            var setupIntentOptions = new SetupIntentCreateOptions
            {
                Customer = customer.Id,
                Metadata = new Dictionary<string, string>
                {
                    { nameof(User), user.ToString("N") }
                }
            };
            var setupIntentService = new SetupIntentService();
            var intent = await setupIntentService.CreateAsync(setupIntentOptions);
            return new OperationResult<SetupIntent>(intent);
        }

        public async Task<SetupIntent> RetriveSetupIntentAsync(string id)
        {
            StripeConfiguration.ApiKey = await this.configuration.Value.GetSettingAsync<string>("STRIPE_KEY");

            if (id.Contains("_secret_"))
            {
                id = id.Split(new[] { "_secret_" }, StringSplitOptions.RemoveEmptyEntries)[0];
            }
            var service = new SetupIntentService();
            return await service.GetAsync(id);
        }

        public async Task<Stripe.Charge> RetriveChargeAsync(string id)
        {
            StripeConfiguration.ApiKey = await this.configuration.Value.GetSettingAsync<string>("STRIPE_KEY");
            var service = new ChargeService();
            return await service.GetAsync(id);
        }

        public async Task<Stripe.Refund> RetriveRefundAsync(string id)
        {
            StripeConfiguration.ApiKey = await this.configuration.Value.GetSettingAsync<string>("STRIPE_KEY");
            var service = new RefundService();
            return await service.GetAsync(id);
        }

        public async Task<Stripe.PaymentMethod> RetrivePaymentMethodAsync(string id)
        {
            StripeConfiguration.ApiKey = await this.configuration.Value.GetSettingAsync<string>("STRIPE_KEY");
            var service = new PaymentMethodService();
            return await service.GetAsync(id);
        }

        public async Task<OperationResult<PaymentIntent>> ChargeAsync(
            Currency currency,
            int amount,
            string reference,
            string customer,
            string paymentMethod,
            int retryCount = 3)
        {
            if (retryCount <= 0)
            {
                return new OperationResult<PaymentIntent>(InternalError.BadGateway);
            }

            StripeConfiguration.ApiKey = await this.configuration.Value.GetSettingAsync<string>("STRIPE_KEY");

            try
            {
                var service = new PaymentIntentService();
                var result = await service.CreateAsync(new PaymentIntentCreateOptions
                {
                    Amount = amount,
                    Currency = currency.ToString().ToLowerInvariant(),
                    Confirm = true,
                    OffSession = true,
                    Customer = customer,
                    PaymentMethod = paymentMethod,
                    CaptureMethod = "automatic",
                    Metadata = new Dictionary<string, string> { { nameof(Order), reference } },
                });
                return new OperationResult<PaymentIntent>(result);

            }
            catch (HttpRequestException)
            {
            }
            catch (OperationCanceledException)
            {
            }
            catch (SocketException)
            {
            }
            catch (IOException)
            {
            }

            return await this.ChargeAsync(currency, amount, reference, customer, paymentMethod, --retryCount);
        }

        public async Task<OperationResult<Refund>> RefundAsync(
            string chargeID,
            long? amount,
            int retryCount = 3)
        {
            if (retryCount <= 0)
            {
                return new OperationResult<Refund>(InternalError.BadGateway);
            }

            StripeConfiguration.ApiKey = await this.configuration.Value.GetSettingAsync<string>("STRIPE_KEY");

            try
            {
                var service = new RefundService();
                var result = await service.CreateAsync(new RefundCreateOptions { Charge = chargeID, Amount = amount });
                return new OperationResult<Refund>(result);
            }
            catch (HttpRequestException)
            {
            }
            catch (OperationCanceledException)
            {
            }
            catch (SocketException)
            {
            }
            catch (IOException)
            {
            }

            return await this.RefundAsync(chargeID, amount, --retryCount);
        }

        public async Task<OperationResult<PaymentIntent>> CompleteAsync(
            string paymentIntentID,
            long? amountToCapture,
            int retryCount = 3)
        {
            if (retryCount <= 0)
            {
                return new OperationResult<PaymentIntent>(InternalError.BadGateway);
            }

            StripeConfiguration.ApiKey = await this.configuration.Value.GetSettingAsync<string>("STRIPE_KEY");

            try
            {
                var service = new PaymentIntentService();
                var result = await service.CaptureAsync(paymentIntentID, options: new PaymentIntentCaptureOptions { AmountToCapture = amountToCapture });
                return new OperationResult<PaymentIntent>(result);

            }
            catch (HttpRequestException)
            {
            }
            catch (OperationCanceledException)
            {
            }
            catch (SocketException)
            {
            }
            catch (IOException)
            {
            }

            return await this.VoidAsync(paymentIntentID, --retryCount);
        }

        public async Task<OperationResult<PaymentIntent>> VoidAsync(
            string paymentIntentID,
            int retryCount = 3)
        {
            if (retryCount <= 0)
            {
                return new OperationResult<PaymentIntent>(InternalError.BadGateway);
            }

            StripeConfiguration.ApiKey = await this.configuration.Value.GetSettingAsync<string>("STRIPE_KEY");

            try
            {
                var service = new PaymentIntentService();
                var result = await service.CancelAsync(paymentIntentID);
                return new OperationResult<PaymentIntent>(result);

            }
            catch (HttpRequestException)
            {
            }
            catch (OperationCanceledException)
            {
            }
            catch (SocketException)
            {
            }
            catch (IOException)
            {
            }

            return await this.VoidAsync(paymentIntentID, --retryCount);
        }

        public async Task<OperationResult<PaymentIntent>> AuthorizeAsync(
            Currency currency,
            int amount,
            string reference,
            string customer,
            string paymentMethod,
            int retryCount = 3)
        {
            if (retryCount <= 0)
            {
                return new OperationResult<PaymentIntent>(InternalError.BadGateway);
            }

            StripeConfiguration.ApiKey = await this.configuration.Value.GetSettingAsync<string>("STRIPE_KEY");

            try
            {
                var service = new PaymentIntentService();
                var result = await service.CreateAsync(new PaymentIntentCreateOptions
                {
                    Amount = amount,
                    Currency = currency.ToString().ToLowerInvariant(),
                    Confirm = true,
                    OffSession = true,
                    Customer = customer,
                    PaymentMethod = paymentMethod,
                    CaptureMethod = "manual",
                    Metadata = new Dictionary<string, string> { { nameof(Order), reference } },
                });
                return new OperationResult<PaymentIntent>(result);

            }
            catch (HttpRequestException)
            {
            }
            catch (OperationCanceledException)
            {
            }
            catch (SocketException)
            {
            }
            catch (IOException)
            {
            }

            return await this.AuthorizeAsync(currency, amount, reference, customer, paymentMethod, --retryCount);
        }
    }
}
