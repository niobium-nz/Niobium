using Stripe;

namespace Cod.Platform.Finance.Stripe
{
    public class StripePaymentProcessor(
        Lazy<IRepository<PaymentMethod>> paymentRepo,
        Lazy<IRepository<Transaction>> transactionRepo,
        Lazy<StripeIntegration> stripeIntegration)
        : IPaymentProcessor
    {
        private static readonly TimeSpan ValidTransactionMaxDelay = TimeSpan.FromMinutes(5);
        public const string PaymentInfoSpliter = "|";

        public virtual async Task<OperationResult<ChargeResponse>> ChargeAsync(ChargeRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (!Support(request))
            {
                return new OperationResult<ChargeResponse>(Cod.InternalError.NotAcceptable);
            }

            if (request.Operation == PaymentOperationKind.Validate)
            {
                var instruction = await stripeIntegration.Value.CreateSetupIntentAsync((Guid)request.Account);
                if (!instruction.IsSuccess)
                {
                    return new OperationResult<ChargeResponse>(instruction);
                }
                else
                {
                    return new OperationResult<ChargeResponse>(
                        new ChargeResponse
                        {
                            Amount = request.Amount,
                            Method = PaymentMethodKind.Visa | PaymentMethodKind.MasterCard | PaymentMethodKind.AmericanExpress,
                            Reference = instruction.Result.CustomerId,
                            UpstreamID = instruction.Result.Id,
                            Instruction = instruction.Result.ClientSecret,
                        });
                }
            }

            var result = new ChargeResponse();
            if (request.Operation == PaymentOperationKind.Refund)
            {
                var transaction = await stripeIntegration.Value.RefundAsync((string)request.Account, request.Amount);
                if (!transaction.IsSuccess)
                {
                    return new OperationResult<ChargeResponse>(transaction);
                }
                result.UpstreamID = transaction.Result.Id;
            }
            else
            {
                OperationResult<PaymentIntent> transaction;

                if (request.Operation == PaymentOperationKind.Complete)
                {
                    transaction = await stripeIntegration.Value.CompleteAsync((string)request.Account, request.Amount);
                    if (!transaction.IsSuccess)
                    {
                        return new OperationResult<ChargeResponse>(transaction);
                    }
                    result.UpstreamID = transaction.Result.LatestChargeId;
                }
                else if (request.Operation == PaymentOperationKind.Void)
                {
                    transaction = await stripeIntegration.Value.VoidAsync((string)request.Account);
                    if (!transaction.IsSuccess)
                    {
                        return new OperationResult<ChargeResponse>(transaction);
                    }
                    result.UpstreamID = transaction.Result.Id;
                }
                else
                {
                    string? stripeCustomer = null;
                    string? stripePaymentMethod = null;
                    
                    if (request.Account != null && request.Account is string paymentInfo)
                    {
                        var paymentInfoParts = paymentInfo.Split([PaymentInfoSpliter], StringSplitOptions.RemoveEmptyEntries);

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
                            return new OperationResult<ChargeResponse>(Cod.InternalError.BadRequest, "Missing customer or payment method information.");
                        }

                        transaction = await stripeIntegration.Value.AuthorizeAsync(
                            request.TargetKind,
                            request.Target,
                            request.Order,
                            request.Currency,
                            request.Amount,
                            request.Reference,
                            stripeCustomer,
                            stripePaymentMethod);
                        if (!transaction.IsSuccess)
                        {
                            return new OperationResult<ChargeResponse>(transaction);
                        }
                        result.UpstreamID = transaction.Result.Id;
                    }
                    else if (request.Operation == PaymentOperationKind.Charge)
                    {
                        transaction = await stripeIntegration.Value.ChargeAsync(
                            request.TargetKind,
                            request.Target,
                            request.Order,
                            request.Currency,
                            request.Amount,
                            request.Reference,
                            stripeCustomer,
                            stripePaymentMethod);
                        if (!transaction.IsSuccess)
                        {
                            return new OperationResult<ChargeResponse>(transaction);
                        }

                        if (transaction.Result.LatestChargeId != null)
                        {
                            // the charge was successful
                            result.UpstreamID = transaction.Result.LatestChargeId;
                        }
                        else
                        {
                            // the charge is pending for some reason, we need to wait for the report
                            // posibly the payment intent is not yet confirmed due to missing customer or payment method information
                            // so we need to return the client secret and other details for further client-side processing (e.g. collecting payment method info / 3D Secure authentication)
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

                if (transaction.Result.LastPaymentError != null)
                {
                    return new OperationResult<ChargeResponse>(Cod.InternalError.PaymentRequired, transaction.Result.LastPaymentError.StripeResponse?.Content);
                }

                result.Reference = request.Reference;
            }

            return new OperationResult<ChargeResponse>(result);
        }

        public virtual async Task<OperationResult<ChargeResult>> ReportAsync(object notification)
        {
            if (notification is not StripeReport r)
            {
                return new OperationResult<ChargeResult>(Cod.InternalError.NotAcceptable);
            }

            if (r.Kind == StripeReportKind.Setup)
            {
                var setup = await stripeIntegration.Value.RetriveSetupIntentAsync(r.ID);
                if (setup == null)
                {
                    return new OperationResult<ChargeResult>(Cod.InternalError.NotFound);
                }

                if (setup.LastSetupError != null)
                {
                    return new OperationResult<ChargeResult>(Cod.InternalError.PaymentRequired, setup.LastSetupError.ErrorDescription);
                }

                if (setup.Status != "succeeded")
                {
                    return new OperationResult<ChargeResult>(Cod.InternalError.PaymentRequired, setup.Status);
                }

                if (setup.ClientSecret != r.Secret)
                {
                    return new OperationResult<ChargeResult>(Cod.InternalError.Forbidden);
                }

                if (DateTime.UtcNow - setup.Created > ValidTransactionMaxDelay)
                {
                    return new OperationResult<ChargeResult>(Cod.InternalError.PreconditionFailed);
                }

                var user = setup.Metadata[Constants.MetadataIntentUserID];
                var pm = await stripeIntegration.Value.RetrivePaymentMethodAsync(setup.PaymentMethodId);
                var pmkey = $"{setup.CustomerId}{PaymentInfoSpliter}{setup.PaymentMethodId}";
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
            if (r.Kind == StripeReportKind.Charge)
            {
                var charge = await stripeIntegration.Value.RetriveChargeAsync(r.ID);
                var targetKind = (ChargeTargetKind)int.Parse(charge.Metadata[Constants.MetadataTargetKindKey]);
                var target = charge.Metadata[Constants.MetadataTargetKey];
                var order = charge.Metadata[Constants.MetadataOrderKey];
                var timestamp = new DateTimeOffset(charge.Created);
                var tpk = Transaction.BuildPartitionKey(target);
                var trk = Transaction.BuildRowKey(timestamp);
                var transaction = await transactionRepo.Value.RetrieveAsync(tpk, trk);

                if (transaction == null)
                {
                    transaction = new Transaction
                    {
                        PartitionKey = tpk,
                        RowKey = trk,
                        Account = charge.PaymentIntentId,
                        Corelation = charge.Id,
                        Reference = order,
                        Delta = charge.AmountCaptured,
                        Provider = (int)PaymentServiceProvider.Stripe,
                        Reason = (int)TransactionReason.Deposit,
                        Status = (int)TransactionStatus.Completed,
                        Remark = Cod.Constants.TRANSACTION_REASON_DEPOSIT,
                    };

                    await transactionRepo.Value.CreateAsync(transaction);
                }

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
            else if (r.Kind == StripeReportKind.Refund)
            {
                var refund = await stripeIntegration.Value.RetriveRefundAsync(r.ID);
                var charge1 = await stripeIntegration.Value.RetriveChargeAsync(refund.ChargeId);
                var targetKind = (ChargeTargetKind)int.Parse(charge1.Metadata[Constants.MetadataTargetKindKey]);
                var target = charge1.Metadata[Constants.MetadataTargetKey];
                var order = charge1.Metadata[Constants.MetadataOrderKey];
                var timestamp = new DateTimeOffset(refund.Created);
                var tpk = Transaction.BuildPartitionKey(target);
                var trk = Transaction.BuildRowKey(timestamp);
                var transaction = await transactionRepo.Value.RetrieveAsync(tpk, trk);

                if (transaction == null)
                {
                    transaction = new Transaction
                    {
                        PartitionKey = tpk,
                        RowKey = trk,
                        Account = charge1.PaymentIntentId,
                        Corelation = refund.Id,
                        Reference = order,
                        Delta = -refund.Amount,
                        Provider = (int)PaymentServiceProvider.Stripe,
                        Reason = (int)TransactionReason.Refund,
                        Status = (int)TransactionStatus.Refunded,
                        Remark = Cod.Constants.TRANSACTION_REASON_REFUND,
                    };

                    await transactionRepo.Value.CreateAsync(transaction);
                }

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
                return new OperationResult<ChargeResult>(Cod.InternalError.NotAcceptable);
            }
        }

        private static bool Support(ChargeRequest request) => request != null && (request.Channel & PaymentChannels.Cards) == PaymentChannels.Cards;

        private static PaymentMethodKind FigurePaymentMethodType(IEnumerable<string> input)
        {
            var result = PaymentMethodKind.None;
            foreach (var item in input)
            {
                result |= FigurePaymentMethodType(item);
            }
            return result;
        }

        private static PaymentMethodKind FigurePaymentMethodType(string input)
            => input.ToUpperInvariant() switch
            {
                "CARD" => PaymentMethodKind.Visa | PaymentMethodKind.MasterCard | PaymentMethodKind.AmericanExpress,
                "AFTERPAY_CLEARPAY" => PaymentMethodKind.Afterpay,
                _ => throw new NotImplementedException(),
            };

        private static PaymentMethodKind FigureCardType(string input) => input.ToUpperInvariant() switch
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

        public async Task<OperationResult<ChargeResult>> RetrieveChargeAsync(string transaction, PaymentChannels paymentChannel)
        {
            if (paymentChannel != PaymentChannels.Cards)
            {
                return new OperationResult<ChargeResult>(Cod.InternalError.NotAcceptable);
            }

            var charge = await stripeIntegration.Value.RetriveChargeAsync(transaction);
            if (charge == null)
            {
                return new OperationResult<ChargeResult>(Cod.InternalError.NotFound);
            }
            
            var targetKind = (ChargeTargetKind)int.Parse(charge.Metadata[Constants.MetadataTargetKindKey]);
            var target = charge.Metadata[Constants.MetadataTargetKey];
            var order = charge.Metadata[Constants.MetadataOrderKey];
            var timestamp = new DateTimeOffset(charge.Created);

            var result = new ChargeResult
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
