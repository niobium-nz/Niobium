using Cod.Finance;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stripe;
using System.Net.Sockets;

namespace Cod.Platform.Finance.Stripe
{
    public class StripeIntegration(IOptions<PaymentServiceOptions> options, ILogger<StripeIntegration> logger)
    {
        public async Task<OperationResult<SetupIntent>> CreateSetupIntentAsync(Guid user)
        {
            var options = new CustomerCreateOptions();
            var service = new CustomerService();
            var customer = await service.CreateAsync(options);

            var setupIntentOptions = new SetupIntentCreateOptions
            {
                Customer = customer.Id,
                Metadata = new Dictionary<string, string>
                {
                    { Constants.MetadataIntentUserID, user.ToString() }
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
                logger.LogError(se, nameof(StripeIntegration));
                return ConvertStripeError<SetupIntent>(se.StripeError);
            }
        }

        public async Task<SetupIntent> RetriveSetupIntentAsync(string id)
        {
            ArgumentNullException.ThrowIfNull(id);

            if (id.Contains("_secret_"))
            {
                id = id.Split(["_secret_"], StringSplitOptions.RemoveEmptyEntries)[0];
            }
            var service = new SetupIntentService();

            return await service.GetAsync(id);
        }

        public async Task<Charge> RetriveChargeAsync(string id)
        {
            var service = new ChargeService();
            return await service.GetAsync(id);
        }

        public async Task<Refund> RetriveRefundAsync(string id)
        {
            var service = new RefundService();
            return await service.GetAsync(id);
        }

        public async Task<global::Stripe.PaymentMethod> RetrivePaymentMethodAsync(string id)
        {
            var service = new PaymentMethodService();
            return await service.GetAsync(id);
        }

        public async Task<OperationResult<PaymentIntent>> ChargeAsync(
            ChargeTargetKind targetKind,
            string target,
            Currency currency,
            long amount,            
            string? order = null,
            string? reference = null,
            string? tenant = null,
            string? stripeCustomerID = null,
            string? stripePaymentMethodID = null,
            int retryCount = 3)
        {
            if (retryCount <= 0)
            {
                return new OperationResult<PaymentIntent>(Cod.InternalError.BadGateway);
            }

            try
            {
                var valueToHash = $"{tenant ?? String.Empty}{target}{order ?? String.Empty}";
                var hash = SHA.SHA256Hash(valueToHash, options.Value.SecretHashKey);
                var metadata = new Dictionary<string, string>
                {
                    { Constants.MetadataTargetKindKey, ((int)targetKind).ToString() },
                    { Constants.MetadataTargetKey, target },
                    { Constants.MetadataHashKey, hash },
                };

                if (order != null)
                {
                    metadata.Add(Constants.MetadataOrderKey, order);
                }

                if (reference != null)
                {
                    metadata.Add(Constants.MetadataReferenceKey, reference);
                }

                if (tenant != null)
                {
                    metadata.Add(Constants.MetadataTenantKey, tenant);
                }

                var intentOptions = new PaymentIntentCreateOptions
                {
                    Amount = amount,
                    Currency = currency.ToString().ToLowerInvariant(),
                    Metadata = metadata,
                };

                if (stripeCustomerID != null)
                {
                    intentOptions.Customer = stripeCustomerID;
                }

                if (stripePaymentMethodID != null)
                {
                    intentOptions.PaymentMethod = stripePaymentMethodID;
                }

                if (stripeCustomerID != null && stripePaymentMethodID != null)
                {
                    intentOptions.OffSession = true;
                    intentOptions.Confirm = true;
                }

                var service = new PaymentIntentService();
                var result = await service.CreateAsync(intentOptions);
                return new OperationResult<PaymentIntent>(result);

            }
            catch (StripeException se)
            {
                logger.LogError(se, nameof(StripeIntegration));
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

            return await ChargeAsync(targetKind, target, currency, amount, order, reference, tenant, stripeCustomerID, stripePaymentMethodID, --retryCount);
        }

        public async Task<OperationResult<Refund>> RefundAsync(
            string chargeID,
            long? amount,
            int retryCount = 3)
        {
            if (retryCount <= 0)
            {
                return new OperationResult<Refund>(Cod.InternalError.BadGateway);
            }

            try
            {
                var service = new RefundService();
                var result = await service.CreateAsync(new RefundCreateOptions { Charge = chargeID, Amount = amount });
                return new OperationResult<Refund>(result);
            }
            catch (StripeException se)
            {
                logger.LogError(se, nameof(StripeIntegration));
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

            return await RefundAsync(chargeID, amount, --retryCount);
        }

        public async Task<OperationResult<PaymentIntent>> CompleteAsync(
            string paymentIntentID,
            long? amountToCapture,
            int retryCount = 3)
        {
            if (retryCount <= 0)
            {
                return new OperationResult<PaymentIntent>(Cod.InternalError.BadGateway);
            }

            try
            {
                var service = new PaymentIntentService();
                var result = await service.CaptureAsync(paymentIntentID, options: new PaymentIntentCaptureOptions { AmountToCapture = amountToCapture });
                return new OperationResult<PaymentIntent>(result);

            }
            catch (StripeException se)
            {
                logger.LogError(se, nameof(StripeIntegration));
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

            return await VoidAsync(paymentIntentID, --retryCount);
        }

        public async Task<OperationResult<PaymentIntent>> VoidAsync(
            string paymentIntentID,
            int retryCount = 3)
        {
            if (retryCount <= 0)
            {
                return new OperationResult<PaymentIntent>(Cod.InternalError.BadGateway);
            }

            try
            {
                var service = new PaymentIntentService();
                var result = await service.CancelAsync(paymentIntentID);
                return new OperationResult<PaymentIntent>(result);

            }
            catch (StripeException se)
            {
                logger.LogError(se, nameof(StripeIntegration));
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

            return await VoidAsync(paymentIntentID, --retryCount);
        }

        public async Task<OperationResult<PaymentIntent>> AuthorizeAsync(
            ChargeTargetKind targetKind,
            string target,
            Currency currency,
            long amount,
            string? order = null,
            string? reference = null,
            string? tenant = null,
            string? stripeCustomerID = null,
            string? stripePaymentMethodID = null,
            int retryCount = 3)
        {
            if (retryCount <= 0)
            {
                return new OperationResult<PaymentIntent>(Cod.InternalError.BadGateway);
            }

            try
            {

                var metadata = new Dictionary<string, string>
                {
                    { Constants.MetadataTargetKindKey, ((int)targetKind).ToString() },
                    { Constants.MetadataTargetKey, target },
                };

                if (order != null)
                {
                    metadata.Add(Constants.MetadataOrderKey, order);
                }

                if (reference != null)
                {
                    metadata.Add(Constants.MetadataReferenceKey, reference);
                }

                if (tenant != null)
                {
                    metadata.Add(Constants.MetadataTenantKey, tenant);
                }

                var service = new PaymentIntentService();
                var result = await service.CreateAsync(new PaymentIntentCreateOptions
                {
                    Amount = amount,
                    Currency = currency.ToString().ToLowerInvariant(),
                    Confirm = true,
                    OffSession = true,
                    Customer = stripeCustomerID,
                    PaymentMethod = stripePaymentMethodID,
                    CaptureMethod = "manual",
                    Metadata = metadata,
                });
                return new OperationResult<PaymentIntent>(result);

            }
            catch (StripeException se)
            {
                logger.LogError(se, nameof(StripeIntegration));
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

            return await AuthorizeAsync(targetKind, target, currency, amount, order, reference, tenant, stripeCustomerID, stripePaymentMethodID, --retryCount);
        }

        private static OperationResult<T> ConvertStripeError<T>(StripeError stripeError)
        {
            if (stripeError == null)
            {
                return new OperationResult<T>(InternalError.PaymentErrorUnknown);
            }

            if (!string.IsNullOrWhiteSpace(stripeError.Code) && !string.IsNullOrWhiteSpace(stripeError.DeclineCode))
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
