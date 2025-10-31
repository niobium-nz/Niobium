using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Niobium.Finance;
using Stripe;
using System.Net.Sockets;

namespace Niobium.Platform.Finance.Stripe
{
    internal sealed class StripeIntegration(ServiceManager serviceManager, IOptions<PaymentServiceOptions> serviceOptions, ILogger<StripeIntegration> logger)
    {
        public async Task<OperationResult<SetupIntent>> CreateSetupIntentAsync(string tenant, Guid user)
        {
            CustomerCreateOptions options = new();
            CustomerService service = serviceManager.GetService<CustomerService>(tenant);
            Customer customer = await service.CreateAsync(options);

            SetupIntentCreateOptions setupIntentOptions = new()
            {
                Customer = customer.Id,
                Metadata = new Dictionary<string, string>
                {
                    { Constants.MetadataIntentUserID, user.ToString() }
                }
            };
            SetupIntentService setupIntentService = serviceManager.GetService<SetupIntentService>(tenant);
            try
            {
                SetupIntent intent = await setupIntentService.CreateAsync(setupIntentOptions);
                return new OperationResult<SetupIntent>(intent);
            }
            catch (StripeException se)
            {
                logger.LogError(se, nameof(StripeIntegration));
                return ConvertStripeError<SetupIntent>(se.StripeError);
            }
        }

        public async Task<SetupIntent> RetriveSetupIntentAsync(string tenant, string id)
        {
            ArgumentNullException.ThrowIfNull(id);

            if (id.Contains("_secret_"))
            {
                id = id.Split(["_secret_"], StringSplitOptions.RemoveEmptyEntries)[0];
            }
            SetupIntentService service = serviceManager.GetService<SetupIntentService>(tenant);
            return await service.GetAsync(id);
        }

        public async Task<Charge> RetriveChargeAsync(string tenant, string id)
        {
            ChargeService service = serviceManager.GetService<ChargeService>(tenant);
            return await service.GetAsync(id);
        }

        public async Task<Refund> RetriveRefundAsync(string tenant, string id)
        {
            RefundService service = serviceManager.GetService<RefundService>(tenant);
            return await service.GetAsync(id);
        }

        public async Task<global::Stripe.PaymentMethod> RetrivePaymentMethodAsync(string tenant, string id)
        {
            PaymentMethodService service = serviceManager.GetService<PaymentMethodService>(tenant);
            return await service.GetAsync(id);
        }

        public async Task<OperationResult<PaymentIntent>> ChargeAsync(
            string tenant,
            ChargeTargetKind targetKind,
            string target,
            Currency currency,
            long amount,
            string? order = null,
            string? reference = null,
            string? stripeCustomerID = null,
            string? stripePaymentMethodID = null,
            int retryCount = 3)
        {
            if (retryCount <= 0)
            {
                return new OperationResult<PaymentIntent>(Niobium.InternalError.BadGateway);
            }

            if (!serviceOptions.Value.Hashes.TryGetValue(tenant, out var hashKey))
            {
                return new OperationResult<PaymentIntent>(Niobium.InternalError.InternalServerError);
            }

            try
            {
                string valueToHash = $"{tenant}{target}{order ?? string.Empty}";
                string hash = SHA.SHA256Hash(valueToHash, hashKey);
                Dictionary<string, string> metadata = new()
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

                metadata.Add(Constants.MetadataTenantKey, tenant);

                PaymentIntentCreateOptions intentOptions = new()
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

                PaymentIntentService service = serviceManager.GetService<PaymentIntentService>(tenant);
                PaymentIntent result = await service.CreateAsync(intentOptions);
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

            return await ChargeAsync(tenant, targetKind, target, currency, amount, order, reference, stripeCustomerID, stripePaymentMethodID, --retryCount);
        }

        public async Task<OperationResult<Refund>> RefundAsync(
            string tenant,
            string chargeID,
            long? amount,
            int retryCount = 3)
        {
            if (retryCount <= 0)
            {
                return new OperationResult<Refund>(Niobium.InternalError.BadGateway);
            }

            try
            {
                RefundService service = serviceManager.GetService<RefundService>(tenant);
                Refund result = await service.CreateAsync(new RefundCreateOptions { Charge = chargeID, Amount = amount });
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

            return await RefundAsync(tenant, chargeID, amount, --retryCount);
        }

        public async Task<OperationResult<PaymentIntent>> CompleteAsync(
            string tenant,
            string paymentIntentID,
            long? amountToCapture,
            int retryCount = 3)
        {
            if (retryCount <= 0)
            {
                return new OperationResult<PaymentIntent>(Niobium.InternalError.BadGateway);
            }

            try
            {
                PaymentIntentService service = serviceManager.GetService<PaymentIntentService>(tenant);
                PaymentIntent result = await service.CaptureAsync(paymentIntentID, options: new PaymentIntentCaptureOptions { AmountToCapture = amountToCapture });
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

            return await VoidAsync(tenant, paymentIntentID, --retryCount);
        }

        public async Task<OperationResult<PaymentIntent>> VoidAsync(
            string tenant,
            string paymentIntentID,
            int retryCount = 3)
        {
            if (retryCount <= 0)
            {
                return new OperationResult<PaymentIntent>(Niobium.InternalError.BadGateway);
            }

            try
            {
                PaymentIntentService service = serviceManager.GetService<PaymentIntentService>(tenant);
                PaymentIntent result = await service.CancelAsync(paymentIntentID);
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

            return await VoidAsync(tenant, paymentIntentID, --retryCount);
        }

        public async Task<OperationResult<PaymentIntent>> AuthorizeAsync(
            string tenant,
            ChargeTargetKind targetKind,
            string target,
            Currency currency,
            long amount,
            string? order = null,
            string? reference = null,
            string? stripeCustomerID = null,
            string? stripePaymentMethodID = null,
            int retryCount = 3)
        {
            if (retryCount <= 0)
            {
                return new OperationResult<PaymentIntent>(Niobium.InternalError.BadGateway);
            }

            try
            {

                Dictionary<string, string> metadata = new()
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

                metadata.Add(Constants.MetadataTenantKey, tenant);

                PaymentIntentService service = serviceManager.GetService<PaymentIntentService>(tenant);
                PaymentIntent result = await service.CreateAsync(new PaymentIntentCreateOptions
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

            return await AuthorizeAsync(tenant, targetKind, target, currency, amount, order, reference, stripeCustomerID, stripePaymentMethodID, --retryCount);
        }

        private static OperationResult<T> ConvertStripeError<T>(StripeError stripeError)
        {
            if (stripeError == null)
            {
                return new OperationResult<T>(InternalError.PaymentErrorUnknown);
            }

            if (!string.IsNullOrWhiteSpace(stripeError.Code) && !string.IsNullOrWhiteSpace(stripeError.DeclineCode))
            {
                string code = stripeError.Code.Trim();
                string declineCode = stripeError.DeclineCode.Trim();
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
