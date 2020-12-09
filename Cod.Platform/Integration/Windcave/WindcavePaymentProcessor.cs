using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Cod.Platform
{
    internal class WindcavePaymentProcessor : IPaymentProcessor
    {
        private static readonly TimeSpan ValidTransactionMaxDelay = TimeSpan.FromMinutes(5);
        private static readonly Dictionary<string, object> CallbackUriParameters = new Dictionary<string, object>
        {
            { Endpoints.ParameterPaymentServiceProvider, PaymentServiceProvider.Windcave },
        };

        private readonly Lazy<IRepository<PaymentMethod>> paymentRepo;
        private readonly Lazy<IRepository<Transaction>> transactionRepo;
        private readonly Lazy<IConfigurationProvider> configuration;
        private readonly Lazy<WindcaveIntegration> windcaveIntegration;
        private readonly ILogger logger;

        public WindcavePaymentProcessor(
            Lazy<IRepository<PaymentMethod>> paymentRepo,
            Lazy<IRepository<Transaction>> transactionRepo,
            Lazy<IConfigurationProvider> configuration,
            Lazy<WindcaveIntegration> windcaveIntegration,
            ILogger logger)
        {
            this.paymentRepo = paymentRepo;
            this.transactionRepo = transactionRepo;
            this.configuration = configuration;
            this.windcaveIntegration = windcaveIntegration;
            this.logger = logger;
        }

        public async Task<OperationResult<ChargeResponse>> ChargeAsync(ChargeRequest request)
        {
            if (!Support(request))
            {
                return new OperationResult<ChargeResponse>(InternalError.NotAcceptable);
            }

            if (request.Account == null)
            {
                var instruction = await this.CreatePaymentSession(request);
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
                            Method = PaymentMethodKind.Visa, // TODO (5he11) 应该指定多个，或者根据卡类型判断
                            Reference = request.Order,
                            Extra = request.Source,
                            UpstreamID = instruction.Result.ID,
                            Instruction = instruction.Result.SubmitCardLink,
                        });
                }
            }

            var apiUri = await this.configuration.Value.GetSettingAsync<string>(Constant.API_URL);
            var callbackUri = new Uri($"{apiUri}/{Endpoints.FormatWindcaveNotification.ToString(CallbackUriParameters)}");
            var result = new ChargeResponse();
            if (request.PaymentKind == PaymentKind.Complete)
            {
                var complete = await this.windcaveIntegration.Value.CompleteTransactionAsync(
                     request.Currency,
                     request.Amount,
                     request.Reference,
                     callbackUri,
                     (string)request.Account);
                if (!complete.IsSuccess)
                {
                    return new OperationResult<ChargeResponse>(complete);
                }

                result.UpstreamID = complete.Result.ID;
                result.Reference = request.Reference;
            }
            else
            {
                var transaction = await this.windcaveIntegration.Value.CreateTransactionAsync(
                     request.PaymentKind,
                     request.Currency,
                     request.Amount,
                     request.Reference,
                     callbackUri,
                     (string)request.Account);
                if (!transaction.IsSuccess)
                {
                    return new OperationResult<ChargeResponse>(transaction);
                }

                result.UpstreamID = transaction.Result.ID;
                result.Reference = transaction.Result.MerchantReference;
            }

            return new OperationResult<ChargeResponse>(result);
        }

        private static bool Support(ChargeRequest request) => request != null && request.Channel == PaymentChannels.CreditCard;

        private async Task<OperationResult<PaymentSession>> CreatePaymentSession(ChargeRequest request)
        {
            if (String.IsNullOrWhiteSpace(request.ReturnUri))
            {
                throw new ArgumentNullException(nameof(request.ReturnUri));
            }

            request.ReturnUri = request.ReturnUri.Trim();
            var returnUriParam = request.ReturnUri.Contains("?") ? "&" : "?";
            var apiUri = await this.configuration.Value.GetSettingAsync<string>(Constant.API_URL);
            var callbackUri = $"{apiUri}/{Endpoints.FormatWindcaveNotification.ToString(CallbackUriParameters)}";
            var session = await this.windcaveIntegration.Value.CreateSessionAsync(
                 request.PaymentKind,
                 request.Currency,
                 request.Amount,
                 request.Reference,
                 new Uri(callbackUri),
                 new Uri($"{request.ReturnUri}{returnUriParam}result=approved"),
                 new Uri($"{request.ReturnUri}{returnUriParam}result=declined"),
                 new Uri($"{request.ReturnUri}{returnUriParam}result=canceled"));
            if (!session.IsSuccess)
            {
                this.logger.LogError($"支付通道上游返回错误: {session.Message} 参考: {session.Reference}");
            }

            return session;
        }

        public async Task<OperationResult<ChargeResult>> ReportAsync(object notification)
        {
            if (!(notification is WindcaveNotification n))
            {
                return new OperationResult<ChargeResult>(InternalError.NotAcceptable);
            }

            Guid user;
            string order = null;
            WindcaveTransaction transaction = null;
            if (n.Kind == WindcaveNotificationKind.Session)
            {
                var session = await this.windcaveIntegration.Value.QuerySessionAsync(n.ID);
                if (!session.IsSuccess)
                {
                    return new OperationResult<ChargeResult>(session);
                }

                user = Guid.Parse(session.Result.MerchantReference);
                var authorized = session.Result.Transactions.SingleOrDefault(t => t.Authorised);
                if (authorized != null)
                {
                    transaction = authorized;
                }
                else
                {
                    return new OperationResult<ChargeResult>(InternalError.PaymentRequired);
                }
            }
            else if (n.Kind == WindcaveNotificationKind.Transaction)
            {
                var query = await this.windcaveIntegration.Value.QueryTransactionAsync(n.ID);
                if (!query.IsSuccess)
                {
                    return new OperationResult<ChargeResult>(query);
                }

                if (query.Result.Authorised)
                {
                    transaction = query.Result;
                    var id = StorageKeyExtensions.ParseFullID(query.Result.MerchantReference);
                    user = Guid.Parse(id.PartitionKey);
                    order = id.RowKey;
                }
            }
            else
            {
                throw new NotImplementedException();
            }

            if (transaction == null)
            {
                return new OperationResult<ChargeResult>(InternalError.NotFound);
            }

            if (DateTimeOffset.UtcNow - transaction.GetTime() > ValidTransactionMaxDelay)
            {
                return new OperationResult<ChargeResult>(InternalError.PreconditionFailed);
            }

            if (n.Kind == WindcaveNotificationKind.Session)
            {
                await this.paymentRepo.Value.CreateAsync(new PaymentMethod
                {
                    PartitionKey = PaymentMethod.BuildPartitionKey(user),
                    RowKey = PaymentMethod.BuildRowKey(transaction.Card.ID),
                    Channel = (int)PaymentChannels.CreditCard,
                    Expires = DateTimeOffset.Parse($"20{transaction.Card.DateExpiryYear}-{transaction.Card.DateExpiryMonth}-28T23:59:59.000Z"),
                    Extra = transaction.Card.CardHolderName,
                    Identity = transaction.Card.CardNumber,
                    Kind = (int)FigureCardType(transaction.Card.Type),
                    Primary = true,
                    Status = (int)PaymentMethodStatus.Valid,
                }, true);
            }

            var status = (transaction.GetKind()) switch
            {
                PaymentKind.Authorize => TransactionStatus.Authorized,
                PaymentKind.Complete => TransactionStatus.Completed,
                PaymentKind.Charge => TransactionStatus.Completed,
                PaymentKind.Void => TransactionStatus.Void,
                PaymentKind.Refund => TransactionStatus.Refunded,
                PaymentKind.Validate => throw new NotImplementedException(),
                _ => throw new NotImplementedException(),
            };

            Transaction t = null;
            if (status == TransactionStatus.Completed || status == TransactionStatus.Refunded)
            {
                var tpk = Transaction.BuildPartitionKey(user.ToKey());
                var trk = Transaction.BuildRowKey(transaction.GetTime());
                t = await this.transactionRepo.Value.GetAsync(tpk, trk);

                if (t == null)
                {
                    t = new Transaction
                    {
                        PartitionKey = user.ToKey(),
                        RowKey = transaction.GetTime().ToReverseUnixTimestamp(),
                        Account = transaction.Card.ID,
                        Corelation = transaction.ID,
                        Reference = order,
                        Delta = Double.Parse(transaction.Amount),
                        Provider = (int)PaymentServiceProvider.Windcave,
                        Reason = (int)TransactionReason.Deposit,
                        Status = (int)status,
                        Remark = Localization.TransactionReason_Deposit,
                    };

                    await this.transactionRepo.Value.CreateAsync(t);
                }
            }

            // TODO (5he11) 这里要挂个HOOK，让外边可以根据Transaction.Reference（订单号）更新订单上边的Transaction字段；上边操作transaction的其实应该按照status决定删除，还是不管，还是插入
            return new OperationResult<ChargeResult>(new ChargeResult
            {
                Account = transaction.Card,
                Amount = transaction.GetAmount(),
                Channel = PaymentChannels.CreditCard,
                Currency = transaction.GetCurrency(),
                PaymentKind = transaction.GetKind(),
                Reference = transaction.MerchantReference,
                Source = transaction.ClientType,
                Target = user.ToKey(),
                TargetKind = ChargeTargetKind.User,
                UpstreamID = transaction.ID,
                AuthorizedAt = transaction.GetTime(),
                Transaction = t,
            });
        }

        private static PaymentMethodKind FigureCardType(string input) => (input.ToLower()) switch
        {
            "visa" => PaymentMethodKind.Visa,
            "mastercard" => PaymentMethodKind.MasterCard,
            "amex" => PaymentMethodKind.AmericanExpress,
            "unionpay" => PaymentMethodKind.UnionPay,
            _ => throw new NotImplementedException(),
        };
    }
}
