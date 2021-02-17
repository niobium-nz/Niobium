using System;
using System.Security.Claims;
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
            }
            else if (request.PaymentKind == PaymentKind.Void)
            {
                transaction = await this.stripeIntegration.Value.VoidAsync((string)request.Account);
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
                }
                else if (request.PaymentKind == PaymentKind.Charge)
                {
                    transaction = await this.stripeIntegration.Value.ChargeAsync(
                     request.Currency,
                     request.Amount,
                     request.Reference,
                     paymentInfoParts[0],
                     paymentInfoParts[1]);
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

        public async virtual Task<OperationResult<ChargeResult>> ReportAsync(object report)
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

            return new OperationResult<ChargeResult>(InternalError.NotAcceptable);
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
