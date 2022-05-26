using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using Stripe;

namespace Cod.Platform
{
    public class StripeIntegration
    {
        private readonly Lazy<IConfigurationProvider> configuration;
        private readonly ILogger logger;

        public StripeIntegration(Lazy<IConfigurationProvider> configuration, ILogger logger)
        {
            this.configuration = configuration;
            this.logger = logger;
        }

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
            try
            {
                var intent = await setupIntentService.CreateAsync(setupIntentOptions);
                return new OperationResult<SetupIntent>(intent);
            }
            catch (StripeException se)
            {
                this.logger.LogError(se, nameof(StripeIntegration));
                return ConvertStripeError<SetupIntent>(se.StripeError);
            }
        }

        public async Task<SetupIntent> RetriveSetupIntentAsync(string id)
        {
            if (id is null)
            {
                throw new ArgumentNullException(nameof(id));
            }

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
#pragma warning disable CA1308 // Stripe standard
                    Currency = currency.ToString().ToLowerInvariant(),
#pragma warning restore CA1308 // Stripe standard
                    Confirm = true,
                    OffSession = true,
                    Customer = customer,
                    PaymentMethod = paymentMethod,
                    CaptureMethod = "automatic",
                    Metadata = new Dictionary<string, string> { { nameof(Order), reference } },
                });
                return new OperationResult<PaymentIntent>(result);

            }
            catch (StripeException se)
            {
                this.logger.LogError(se, nameof(StripeIntegration));
                return ConvertStripeError<PaymentIntent>(se.StripeError);
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
            catch (StripeException se)
            {
                this.logger.LogError(se, nameof(StripeIntegration));
                return ConvertStripeError<Refund>(se.StripeError);
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
            catch (StripeException se)
            {
                this.logger.LogError(se, nameof(StripeIntegration));
                return ConvertStripeError<PaymentIntent>(se.StripeError);
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
            catch (StripeException se)
            {
                this.logger.LogError(se, nameof(StripeIntegration));
                return ConvertStripeError<PaymentIntent>(se.StripeError);
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
#pragma warning disable CA1308 // Stripe standard
                    Currency = currency.ToString().ToLowerInvariant(),
#pragma warning restore CA1308 // Stripe standard
                    Confirm = true,
                    OffSession = true,
                    Customer = customer,
                    PaymentMethod = paymentMethod,
                    CaptureMethod = "manual",
                    Metadata = new Dictionary<string, string> { { nameof(Order), reference } },
                });
                return new OperationResult<PaymentIntent>(result);

            }
            catch (StripeException se)
            {
                this.logger.LogError(se, nameof(StripeIntegration));
                return ConvertStripeError<PaymentIntent>(se.StripeError);
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

        private static OperationResult<T> ConvertStripeError<T>(StripeError stripeError)
        {
            if (stripeError == null)
            {
                return new OperationResult<T>(InternalError.PaymentErrorUnknown);
            }

            if (!String.IsNullOrWhiteSpace(stripeError.Code) && !String.IsNullOrWhiteSpace(stripeError.DeclineCode))
            {
                var code = stripeError.Code.Trim();
                var declineCode = stripeError.DeclineCode.Trim();
                if (code == "card_declined" && declineCode == "incorrect_cvc")
                {
                    return new OperationResult<T>(InternalError.PaymentErrorIncorrectCVC);
                }
                else if (code == "card_declined" && declineCode == "expired_card")
                {
                    return new OperationResult<T>(InternalError.PaymentErrorExpiredCard);
                }
                else if (code == "card_declined" && declineCode == "insufficient_funds")
                {
                    return new OperationResult<T>(InternalError.PaymentErrorInsufficientFunds);
                }
            }

            return new OperationResult<T>(InternalError.PaymentErrorUnknown);
        }
    }
}
