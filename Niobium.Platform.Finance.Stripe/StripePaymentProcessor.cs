using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Niobium.Finance;
using Stripe;
using PaymentMethod = Niobium.Finance.PaymentMethod;

namespace Niobium.Platform.Finance.Stripe
{
    public class StripePaymentProcessor(
        Lazy<IRepository<PaymentMethod>> paymentRepo,
        Lazy<IRepository<Transaction>> transactionRepo,
        Lazy<StripeIntegration> stripeIntegration,
        IOptions<PaymentServiceOptions> options,
        ILogger<StripePaymentProcessor> logger)
        : IPaymentProcessor
    {
        private static readonly TimeSpan ValidTransactionMaxDelay = TimeSpan.FromMinutes(5);
        public const string PaymentInfoSpliter = "|";

        public virtual async Task<OperationResult<ChargeResponse>> ChargeAsync(ChargeRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (!Support(request))
            {
                return new OperationResult<ChargeResponse>(Niobium.InternalError.NotAcceptable);
            }

            if (request.Operation == PaymentOperationKind.Validate)
            {
                if (request.Account == null)
                {
                    return new OperationResult<ChargeResponse>(Niobium.InternalError.BadRequest, "Account information is required for validation.");
                }

                OperationResult<SetupIntent> instruction = await stripeIntegration.Value.CreateSetupIntentAsync((Guid)request.Account);
                return !instruction.IsSuccess
                    ? new OperationResult<ChargeResponse>(instruction)
                    : new OperationResult<ChargeResponse>(
                        new ChargeResponse
                        {
                            Amount = request.Amount,
                            Method = PaymentMethodKind.Visa | PaymentMethodKind.MasterCard | PaymentMethodKind.AmericanExpress,
                            Reference = instruction.Result?.CustomerId,
                            UpstreamID = instruction.Result?.Id,
                            Instruction = instruction.Result?.ClientSecret,
                        });
            }

            ChargeResponse result = new();
            if (request.Operation == PaymentOperationKind.Refund)
            {
                if (request.Account == null)
                {
                    return new OperationResult<ChargeResponse>(Niobium.InternalError.BadRequest, "Account information is required for refund operation.");
                }

                OperationResult<Refund> transaction = await stripeIntegration.Value.RefundAsync((string)request.Account, request.Amount);
                if (!transaction.IsSuccess)
                {
                    return new OperationResult<ChargeResponse>(transaction);
                }
                result.UpstreamID = transaction.Result?.Id;
            }
            else
            {
                OperationResult<PaymentIntent> transaction;

                if (request.Operation == PaymentOperationKind.Complete)
                {
                    if (request.Account == null)
                    {
                        return new OperationResult<ChargeResponse>(Niobium.InternalError.BadRequest, "Account information is required for completion operation.");
                    }

                    transaction = await stripeIntegration.Value.CompleteAsync((string)request.Account, request.Amount);
                    if (!transaction.IsSuccess)
                    {
                        return new OperationResult<ChargeResponse>(transaction);
                    }
                    result.UpstreamID = transaction.Result?.LatestChargeId;
                }
                else if (request.Operation == PaymentOperationKind.Void)
                {
                    if (request.Account == null)
                    {
                        return new OperationResult<ChargeResponse>(Niobium.InternalError.BadRequest, "Account information is required for void operation.");
                    }

                    transaction = await stripeIntegration.Value.VoidAsync((string)request.Account);
                    if (!transaction.IsSuccess)
                    {
                        return new OperationResult<ChargeResponse>(transaction);
                    }
                    result.UpstreamID = transaction.Result?.Id;
                }
                else
                {
                    string? stripeCustomer = null;
                    string? stripePaymentMethod = null;

                    if (request.Account is not null and string paymentInfo)
                    {
                        string[] paymentInfoParts = paymentInfo.Split([PaymentInfoSpliter], StringSplitOptions.RemoveEmptyEntries);

                        if (paymentInfoParts.Length >= 1)
                        {
                            stripeCustomer = paymentInfoParts[0];
                        }

                        if (paymentInfoParts.Length >= 2)
                        {
                            stripePaymentMethod = paymentInfoParts[1];
                        }
                    }

                    if (request.Operation == PaymentOperationKind.Authorize)
                    {
                        if (string.IsNullOrWhiteSpace(stripeCustomer) || string.IsNullOrWhiteSpace(stripePaymentMethod))
                        {
                            return new OperationResult<ChargeResponse>(Niobium.InternalError.BadRequest, "Missing customer or payment method information.");
                        }

                        transaction = await stripeIntegration.Value.AuthorizeAsync(
                            request.TargetKind,
                            request.Target,
                            request.Currency,
                            request.Amount,
                            order: request.Order,
                            reference: request.Reference,
                            tenant: request.Tenant,
                            stripeCustomerID: stripeCustomer,
                            stripePaymentMethodID: stripePaymentMethod);
                        if (!transaction.IsSuccess)
                        {
                            return new OperationResult<ChargeResponse>(transaction);
                        }
                        result.UpstreamID = transaction.Result?.Id;
                    }
                    else if (request.Operation == PaymentOperationKind.Charge)
                    {
                        transaction = await stripeIntegration.Value.ChargeAsync(
                            request.TargetKind,
                            request.Target,
                            request.Currency,
                            request.Amount,
                            order: request.Order,
                            reference: request.Reference,
                            tenant: request.Tenant,
                            stripeCustomerID: stripeCustomer,
                            stripePaymentMethodID: stripePaymentMethod);
                        if (!transaction.IsSuccess)
                        {
                            return new OperationResult<ChargeResponse>(transaction);
                        }

                        if (transaction.Result?.LatestChargeId != null)
                        {
                            // the charge was successful
                            result.UpstreamID = transaction.Result.LatestChargeId;
                        }
                        else
                        {
                            // the charge is pending for some reason, we need to wait for the report
                            // posibly the payment intent is not yet confirmed due to missing customer or payment method information
                            // so we need to return the client secret and other details for further client-side processing (e.g. collecting payment method info / 3D Secure authentication)
                            if (transaction.Result == null)
                            {
                                throw new InvalidOperationException("Transaction result is null, expected a valid PaymentIntent.");
                            }

                            result.Amount = transaction.Result.Amount;
                            result.Method = FigurePaymentMethodType(transaction.Result.PaymentMethodTypes);
                            result.Instruction = transaction.Result.ClientSecret;
                            result.UpstreamID = transaction.Result.Id;
                        }
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
                }

                if (!transaction.IsSuccess)
                {
                    return new OperationResult<ChargeResponse>(transaction);
                }

                if (transaction.Result?.LastPaymentError != null)
                {
                    return new OperationResult<ChargeResponse>(Niobium.InternalError.PaymentRequired, transaction.Result.LastPaymentError.StripeResponse?.Content);
                }

                result.Reference = request.Reference;
            }

            return new OperationResult<ChargeResponse>(result);
        }

        public virtual async Task<OperationResult<ChargeResult>> ReportAsync(string notificationJSON)
        {
            if (string.IsNullOrWhiteSpace(notificationJSON))
            {
                return new OperationResult<ChargeResult>(Niobium.InternalError.NotAcceptable);
            }

            StripeReport? r = JsonMarshaller.Unmarshall<StripeReport>(notificationJSON);

            if (r == null || !StripeReport.SupportedTypes.Contains(r.Type))
            {
                return new OperationResult<ChargeResult>(Niobium.InternalError.NotAcceptable);
            }

            if (r.Type == StripeReport.TypeSetup)
            {
                SetupIntent setup = await StripeIntegration.RetriveSetupIntentAsync(r.Data.Object.ID);
                if (setup == null)
                {
                    return new OperationResult<ChargeResult>(Niobium.InternalError.NotFound);
                }

                if (setup.LastSetupError != null)
                {
                    return new OperationResult<ChargeResult>(Niobium.InternalError.PaymentRequired, setup.LastSetupError.ErrorDescription);
                }

                if (setup.Status != "succeeded")
                {
                    return new OperationResult<ChargeResult>(Niobium.InternalError.PaymentRequired, setup.Status);
                }

                if (DateTime.UtcNow - setup.Created > ValidTransactionMaxDelay)
                {
                    return new OperationResult<ChargeResult>(Niobium.InternalError.PreconditionFailed);
                }

                string user = setup.Metadata[Constants.MetadataIntentUserID];
                global::Stripe.PaymentMethod pm = await StripeIntegration.RetrivePaymentMethodAsync(setup.PaymentMethodId);
                string pmkey = $"{setup.CustomerId}{PaymentInfoSpliter}{setup.PaymentMethodId}";
                await paymentRepo.Value.CreateAsync(new PaymentMethod
                {
                    PartitionKey = PaymentMethod.BuildPartitionKey(user),
                    RowKey = PaymentMethod.BuildRowKey(pmkey),
                    Channel = (int)PaymentChannels.Cards,
                    Expires = DateTimeOffset.Parse($"{pm.Card.ExpYear}-{pm.Card.ExpMonth}-28T23:59:59.000Z"),
                    Extra = pm.BillingDetails.Name,
                    Identity = pm.Card.Last4,
                    Kind = (int)FigureCardType(pm.Card.Brand),
                    Primary = true,
                    Status = (int)PaymentMethodStatus.Valid,
                }, true);

                return new OperationResult<ChargeResult>(new ChargeResult
                {
                    Account = setup.PaymentMethodId,
                    Amount = 0,
                    Channel = PaymentChannels.Cards,
                    PaymentKind = PaymentOperationKind.Validate,
                    Reference = setup.CustomerId,
                    Target = user,
                    TargetKind = ChargeTargetKind.User,
                    UpstreamID = setup.Id,
                    AuthorizedAt = setup.Created,
                });
            }

            // TODO (5he11) 这里要挂个HOOK，让外边可以根据Transaction.Reference（订单号）更新订单上边的Transaction字段；上边操作transaction的其实应该按照status决定删除，还是不管，还是插入
            if (r.Type == StripeReport.TypeCharge)
            {
                Charge charge = await StripeIntegration.RetriveChargeAsync(r.Data.Object.ID);
                ChargeTargetKind targetKind = (ChargeTargetKind)int.Parse(charge.Metadata[Constants.MetadataTargetKindKey]);
                string target = charge.Metadata[Constants.MetadataTargetKey];
                charge.Metadata.TryGetValue(Constants.MetadataOrderKey, out string? order);
                charge.Metadata.TryGetValue(Constants.MetadataTenantKey, out string? tenant);
                string paymentHash = charge.Metadata[Constants.MetadataHashKey];
                string valueToHash = $"{tenant ?? string.Empty}{target}{order ?? string.Empty}";
                string serverHash = SHA.SHA256Hash(valueToHash, options.Value.SecretHashKey);
                if (!serverHash.Equals(paymentHash, StringComparison.OrdinalIgnoreCase))
                {
                    logger.LogWarning($"Payment hash mismatch for charge {charge.Id}. Expected: {serverHash}, Received: {paymentHash}");
                    return new OperationResult<ChargeResult>(Niobium.InternalError.NotAcceptable);
                }

                DateTimeOffset timestamp = new(charge.Created);
                string tpk = Transaction.BuildPartitionKey(target);
                string trk = Transaction.BuildRowKey(timestamp);
                Transaction? transaction = await transactionRepo.Value.RetrieveAsync(tpk, trk);

                transaction ??= new Transaction
                {
                    PartitionKey = tpk,
                    RowKey = trk,
                    Account = charge.PaymentIntentId,
                    Corelation = charge.Id,
                    Reference = order,
                    Tenant = tenant,
                    Delta = charge.AmountCaptured,
                    Provider = (int)PaymentServiceProvider.Stripe,
                    Reason = (int)TransactionReason.Deposit,
                    Status = (int)TransactionStatus.Completed,
                    Remark = Niobium.Constants.TransactionReasonDeposit,
                };

                return new OperationResult<ChargeResult>(new ChargeResult
                {
                    Account = transaction.Account,
                    Amount = charge.AmountCaptured,
                    Channel = PaymentChannels.Cards,
                    Currency = Currency.Parse(charge.Currency),
                    PaymentKind = PaymentOperationKind.Complete,
                    Reference = transaction.Reference,
                    Target = target,
                    TargetKind = targetKind,
                    UpstreamID = charge.Id,
                    AuthorizedAt = timestamp,
                    Transaction = transaction,
                });
            }
            else if (r.Type == StripeReport.TypeRefund)
            {
                Refund refund = await StripeIntegration.RetriveRefundAsync(r.Data.Object.ID);
                Charge charge1 = await StripeIntegration.RetriveChargeAsync(refund.ChargeId);

                ChargeTargetKind targetKind = (ChargeTargetKind)int.Parse(charge1.Metadata[Constants.MetadataTargetKindKey]);
                string target = charge1.Metadata[Constants.MetadataTargetKey];
                charge1.Metadata.TryGetValue(Constants.MetadataOrderKey, out string? order);
                charge1.Metadata.TryGetValue(Constants.MetadataTenantKey, out string? tenant);
                string paymentHash = charge1.Metadata[Constants.MetadataHashKey];
                string valueToHash = $"{tenant ?? string.Empty}{target}{order ?? string.Empty}";
                string serverHash = SHA.SHA256Hash(valueToHash, options.Value.SecretHashKey);
                if (!serverHash.Equals(paymentHash, StringComparison.OrdinalIgnoreCase))
                {
                    logger.LogWarning($"Payment hash mismatch for charge {charge1.Id}. Expected: {serverHash}, Received: {paymentHash}");
                    return new OperationResult<ChargeResult>(Niobium.InternalError.NotAcceptable);
                }

                DateTimeOffset timestamp = new(refund.Created);
                string tpk = Transaction.BuildPartitionKey(target);
                string trk = Transaction.BuildRowKey(timestamp);
                Transaction? transaction = await transactionRepo.Value.RetrieveAsync(tpk, trk);

                transaction ??= new Transaction
                {
                    PartitionKey = tpk,
                    RowKey = trk,
                    Account = charge1.PaymentIntentId,
                    Corelation = refund.Id,
                    Reference = order,
                    Tenant = tenant,
                    Delta = -refund.Amount,
                    Provider = (int)PaymentServiceProvider.Stripe,
                    Reason = (int)TransactionReason.Refund,
                    Status = (int)TransactionStatus.Refunded,
                    Remark = Niobium.Constants.TransactionReasonRefund,
                };

                return new OperationResult<ChargeResult>(new ChargeResult
                {
                    Account = transaction.Account,
                    Amount = -(int)refund.Amount,
                    Channel = PaymentChannels.Cards,
                    Currency = Currency.Parse(refund.Currency),
                    PaymentKind = PaymentOperationKind.Complete,
                    Reference = transaction.Reference,
                    Target = target,
                    TargetKind = targetKind,
                    UpstreamID = refund.Id,
                    AuthorizedAt = timestamp,
                    Transaction = transaction,
                });
            }
            else
            {
                return new OperationResult<ChargeResult>(Niobium.InternalError.NotAcceptable);
            }
        }

        private static bool Support(ChargeRequest request)
        {
            return request != null && (request.Channel & PaymentChannels.Cards) == PaymentChannels.Cards;
        }

        private static PaymentMethodKind FigurePaymentMethodType(IEnumerable<string> input)
        {
            PaymentMethodKind result = PaymentMethodKind.None;
            foreach (string item in input)
            {
                result |= FigurePaymentMethodType(item);
            }
            return result;
        }

        private static PaymentMethodKind FigurePaymentMethodType(string input)
        {
            return input.ToUpperInvariant() switch
            {
                "CARD" => PaymentMethodKind.Visa | PaymentMethodKind.MasterCard | PaymentMethodKind.AmericanExpress,
                "AFTERPAY_CLEARPAY" => PaymentMethodKind.Afterpay,
                _ => throw new NotImplementedException(),
            };
        }

        private static PaymentMethodKind FigureCardType(string input)
        {
            return input.ToUpperInvariant() switch
            {
                "VISA" => PaymentMethodKind.Visa,
                "MASTERCARD" => PaymentMethodKind.MasterCard,
                "AMEX" => PaymentMethodKind.AmericanExpress,
                "UNIONPAY" => PaymentMethodKind.UnionPay,
                "DINERS" => PaymentMethodKind.DinnersClub,
                "DISCOVER" => PaymentMethodKind.Discover,
                "JCB" => PaymentMethodKind.JCB,
                _ => throw new NotImplementedException(),
            };
        }

        public async Task<OperationResult<ChargeResult>> RetrieveChargeAsync(string transaction, PaymentChannels paymentChannel)
        {
            if (paymentChannel != PaymentChannels.Cards)
            {
                return new OperationResult<ChargeResult>(Niobium.InternalError.NotAcceptable);
            }

            Charge charge = await StripeIntegration.RetriveChargeAsync(transaction);
            if (charge == null)
            {
                return new OperationResult<ChargeResult>(Niobium.InternalError.NotFound);
            }

            ChargeTargetKind targetKind = (ChargeTargetKind)int.Parse(charge.Metadata[Constants.MetadataTargetKindKey]);
            string target = charge.Metadata[Constants.MetadataTargetKey];
            string order = charge.Metadata[Constants.MetadataOrderKey];
            DateTimeOffset timestamp = new(charge.Created);

            ChargeResult result = new()
            {
                Account = charge.PaymentIntentId,
                Amount = (int)charge.AmountCaptured,
                Channel = PaymentChannels.Cards,
                Currency = Currency.Parse(charge.Currency),
                PaymentKind = PaymentOperationKind.Charge,
                Reference = order,
                Target = target,
                TargetKind = targetKind,
                UpstreamID = charge.Id,
                AuthorizedAt = timestamp,
            };

            return new OperationResult<ChargeResult>(result);
        }
    }
}
