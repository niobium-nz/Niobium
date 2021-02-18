using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Stripe;

namespace Cod.Platform
{
    public class StripePaymentProcessor : IPaymentProcessor
    {
        private static readonly TimeSpan ValidTransactionMaxDelay = TimeSpan.FromMinutes(5);
        public const string PaymentInfoSpliter = "|";
        private readonly Lazy<IRepository<PaymentMethod>> paymentRepo;
        private readonly Lazy<IRepository<Transaction>> transactionRepo;
        private readonly Lazy<IConfigurationProvider> configuration;
        private readonly Lazy<StripeIntegration> stripeIntegration;
        private readonly ILogger logger;

        public StripePaymentProcessor(
            Lazy<IRepository<PaymentMethod>> paymentRepo,
            Lazy<IRepository<Transaction>> transactionRepo,
            Lazy<IConfigurationProvider> configuration,
            Lazy<StripeIntegration> stripeIntegration,
            ILogger logger)
        {
            this.paymentRepo = paymentRepo;
            this.transactionRepo = transactionRepo;
            this.configuration = configuration;
            this.stripeIntegration = stripeIntegration;
            this.logger = logger;
        }

        public virtual async Task<OperationResult<ChargeResponse>> ChargeAsync(ChargeRequest request)
        {
            if (!Support(request))
            {
                return new OperationResult<ChargeResponse>(InternalError.NotAcceptable);
            }

            if (request.PaymentKind == PaymentKind.Validate)
            {
                var instruction = await this.stripeIntegration.Value.CreateSetupIntentAsync((Guid)request.Account);
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
            OperationResult<PaymentIntent> transaction;

            if (request.PaymentKind == PaymentKind.Complete)
            {
                transaction = await this.stripeIntegration.Value.CompleteAsync((string)request.Account, request.Amount);
                result.UpstreamID = transaction.Result.Charges.ToList().Where(c => c.AmountCaptured == request.Amount).OrderByDescending(c => c.Created).First().Id;
            }
            else if (request.PaymentKind == PaymentKind.Void)
            {
                transaction = await this.stripeIntegration.Value.VoidAsync((string)request.Account);
                result.UpstreamID = transaction.Result.Id;
            }
            else
            {
                var paymentInfo = (string)request.Account;
                var paymentInfoParts = paymentInfo.Split(new[] { PaymentInfoSpliter }, StringSplitOptions.RemoveEmptyEntries);
                if (paymentInfoParts.Length != 2)
                {
                    throw new ArgumentOutOfRangeException(nameof(request));
                }

                if (request.PaymentKind == PaymentKind.Authorize)
                {
                    transaction = await this.stripeIntegration.Value.AuthorizeAsync(
                     request.Currency,
                     request.Amount,
                     request.Reference,
                     paymentInfoParts[0],
                     paymentInfoParts[1]);
                    result.UpstreamID = transaction.Result.Id;
                }
                else if (request.PaymentKind == PaymentKind.Charge)
                {
                    transaction = await this.stripeIntegration.Value.ChargeAsync(
                     request.Currency,
                     request.Amount,
                     request.Reference,
                     paymentInfoParts[0],
                     paymentInfoParts[1]);
                    result.UpstreamID = transaction.Result.Charges.ToList().Where(c => c.AmountCaptured == request.Amount).OrderByDescending(c => c.Created).First().Id;
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
                return new OperationResult<ChargeResponse>(InternalError.PaymentRequired, transaction.Result.LastPaymentError.StripeResponse?.Content);
            }

            result.UpstreamID = transaction.Result.Id;
            result.Reference = request.Reference;

            return new OperationResult<ChargeResponse>(result);
        }

        public virtual async Task<OperationResult<ChargeResult>> ReportAsync(object report)
        {
            if (!(report is StripeReport r))
            {
                return new OperationResult<ChargeResult>(InternalError.NotAcceptable);
            }

            if (r.Kind == StripeReportKind.Setup)
            {
                var setup = await this.stripeIntegration.Value.RetriveSetupIntentAsync(r.ID);
                if (setup == null)
                {
                    return new OperationResult<ChargeResult>(InternalError.NotFound);
                }

                if (setup.LastSetupError != null)
                {
                    return new OperationResult<ChargeResult>(InternalError.PaymentRequired, setup.LastSetupError.ErrorDescription);
                }

                if (setup.Status != "succeeded")
                {
                    return new OperationResult<ChargeResult>(InternalError.PaymentRequired, setup.Status);
                }

                if (setup.ClientSecret != r.Secret)
                {
                    return new OperationResult<ChargeResult>(InternalError.Forbidden);
                }

                if (DateTime.UtcNow - setup.Created > ValidTransactionMaxDelay)
                {
                    return new OperationResult<ChargeResult>(InternalError.PreconditionFailed);
                }

                var user = Guid.Parse(setup.Metadata[nameof(User)]);
                var pm = await this.stripeIntegration.Value.RetrivePaymentMethodAsync(setup.PaymentMethodId);
                var pmkey = $"{setup.CustomerId}{PaymentInfoSpliter}{setup.PaymentMethodId}";
                await this.paymentRepo.Value.CreateAsync(new PaymentMethod
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
                    Account = pmkey,
                    Amount = 0,
                    Channel = PaymentChannels.Cards,
                    PaymentKind = PaymentKind.Validate,
                    Reference = pmkey,
                    Target = user.ToKey(),
                    TargetKind = ChargeTargetKind.User,
                    UpstreamID = setup.Id,
                    AuthorizedAt = setup.Created,
                });
            }

            // TODO (5he11) 这里要挂个HOOK，让外边可以根据Transaction.Reference（订单号）更新订单上边的Transaction字段；上边操作transaction的其实应该按照status决定删除，还是不管，还是插入
            if (r.Kind == StripeReportKind.Charge)
            {
                var charge = await this.stripeIntegration.Value.RetriveChargeAsync(r.ID);
                var reference = charge.Metadata[nameof(Order)];
                var key = StorageKeyExtensions.ParseFullID(reference);
                var user = Guid.Parse(key.PartitionKey);
                var order = key.RowKey;
                var timestamp = new DateTimeOffset(charge.Created);
                var tpk = Transaction.BuildPartitionKey(user.ToKey());
                var trk = Transaction.BuildRowKey(timestamp);
                var transaction = await this.transactionRepo.Value.GetAsync(tpk, trk);

                if (transaction == null)
                {
                    transaction = new Transaction
                    {
                        PartitionKey = user.ToKey(),
                        RowKey = timestamp.ToReverseUnixTimestamp(),
                        Account = $"{charge.CustomerId}{PaymentInfoSpliter}{charge.PaymentMethod}",
                        Corelation = charge.Id,
                        Reference = order,
                        Delta = charge.AmountCaptured / 100d,
                        Provider = (int)PaymentServiceProvider.Stripe,
                        Reason = (int)TransactionReason.Deposit,
                        Status = (int)TransactionStatus.Completed,
                        Remark = Constant.TRANSACTION_REASON_DEPOSIT,
                    };

                    await this.transactionRepo.Value.CreateAsync(transaction);
                }

                return new OperationResult<ChargeResult>(new ChargeResult
                {
                    Account = transaction.Account,
                    Amount = (int)charge.AmountCaptured,
                    Channel = PaymentChannels.Cards,
                    Currency = Currency.Parse(charge.Currency),
                    PaymentKind =  PaymentKind.Complete,
                    Reference = reference,
                    Target = user.ToKey(),
                    TargetKind = ChargeTargetKind.User,
                    UpstreamID = charge.Id,
                    AuthorizedAt = timestamp,
                    Transaction = transaction,
                });
            }
            else if (r.Kind == StripeReportKind.Refund)
            {
                var refund = await this.stripeIntegration.Value.RetriveRefundAsync(r.ID);
                var charge1 = await this.stripeIntegration.Value.RetriveChargeAsync(refund.ChargeId);
                var reference = charge1.Metadata[nameof(Order)];
                var key = StorageKeyExtensions.ParseFullID(reference);
                var user = Guid.Parse(key.PartitionKey);
                var order = key.RowKey;
                var timestamp = new DateTimeOffset(refund.Created);
                var tpk = Transaction.BuildPartitionKey(user.ToKey());
                var trk = Transaction.BuildRowKey(timestamp);
                var transaction = await this.transactionRepo.Value.GetAsync(tpk, trk);

                if (transaction == null)
                {
                    transaction = new Transaction
                    {
                        PartitionKey = user.ToKey(),
                        RowKey = timestamp.ToReverseUnixTimestamp(),
                        Account = $"{charge1.CustomerId}{PaymentInfoSpliter}{charge1.PaymentIntentId}",
                        Corelation = refund.Id,
                        Reference = order,
                        Delta = -refund.Amount / 100d,
                        Provider = (int)PaymentServiceProvider.Stripe,
                        Reason = (int)TransactionReason.Refund,
                        Status = (int)TransactionStatus.Refunded,
                        Remark = Constant.TRANSACTION_REASON_REFUND,
                    };

                    await this.transactionRepo.Value.CreateAsync(transaction);
                }

                return new OperationResult<ChargeResult>(new ChargeResult
                {
                    Account = transaction.Account,
                    Amount = -(int)refund.Amount,
                    Channel = PaymentChannels.Cards,
                    Currency = Currency.Parse(refund.Currency),
                    PaymentKind = PaymentKind.Complete,
                    Reference = reference,
                    Target = user.ToKey(),
                    TargetKind = ChargeTargetKind.User,
                    UpstreamID = refund.Id,
                    AuthorizedAt = timestamp,
                    Transaction = transaction,
                });
            }
            else
            {
                return new OperationResult<ChargeResult>(InternalError.NotAcceptable);
            }
        }

        private static bool Support(ChargeRequest request) => request != null && request.Channel == PaymentChannels.Cards;

        private static PaymentMethodKind FigureCardType(string input) => (input.ToLower()) switch
        {
            "visa" => PaymentMethodKind.Visa,
            "mastercard" => PaymentMethodKind.MasterCard,
            "amex" => PaymentMethodKind.AmericanExpress,
            "unionpay" => PaymentMethodKind.UnionPay,
            "diners" => PaymentMethodKind.DinnersClub,
            "discover" => PaymentMethodKind.Discover,
            "jcb" => PaymentMethodKind.JCB,
            _ => throw new NotImplementedException(),
        };
    }
}
