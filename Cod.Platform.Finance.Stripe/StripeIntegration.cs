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
            CustomerCreateOptions options = new();
            CustomerService service = new();
            Customer customer = await service.CreateAsync(options);

            SetupIntentCreateOptions setupIntentOptions = new()
            {
                Customer = customer.Id,
                Metadata = new Dictionary<string, string>
                {
                    { Constants.MetadataIntentUserID, user.ToString() }
                }
            };
            SetupIntentService setupIntentService = new();
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

        public static async Task<SetupIntent> RetriveSetupIntentAsync(string id)
        {
            ArgumentNullException.ThrowIfNull(id);

            if (id.Contains("_secret_"))
            {
                id = id.Split(["_secret_"], StringSplitOptions.RemoveEmptyEntries)[0];
            }
            SetupIntentService service = new();

            return await service.GetAsync(id);
        }

        public static async Task<Charge> RetriveChargeAsync(string id)
        {
            ChargeService service = new();
            return await service.GetAsync(id);
        }

        public static async Task<Refund> RetriveRefundAsync(string id)
        {
            RefundService service = new();
            return await service.GetAsync(id);
        }

        public static async Task<global::Stripe.PaymentMethod> RetrivePaymentMethodAsync(string id)
        {
            PaymentMethodService service = new();
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
                string valueToHash = $"{tenant ?? string.Empty}{target}{order ?? string.Empty}";
                string hash = SHA.SHA256Hash(valueToHash, options.Value.SecretHashKey);
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

                if (tenant != null)
                {
                    metadata.Add(Constants.MetadataTenantKey, tenant);
                }

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

                PaymentIntentService service = new();
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
                RefundService service = new();
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
                PaymentIntentService service = new();
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
                PaymentIntentService service = new();
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

                if (tenant != null)
                {
                    metadata.Add(Constants.MetadataTenantKey, tenant);
                }

                PaymentIntentService service = new();
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
