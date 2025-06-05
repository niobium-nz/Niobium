using Microsoft.Extensions.Logging;

namespace Cod.Platform.Finance
{
    public abstract class AccountableDomain<T> : GenericDomain<T>, IAccountable where T : class
    {
        private const string FrozenKey = "frozen";
        private const string DeltaKey = "delta";
        private readonly Lazy<IQueryableRepository<Transaction>> transactionRepo;
        private readonly Lazy<IQueryableRepository<Accounting>> accountingRepo;
        private readonly Lazy<IEnumerable<IAccountingAuditor>> auditors;
        private readonly Lazy<ICacheStore> cacheStore;
        private readonly ILogger logger;

        public AccountableDomain(
            Lazy<IRepository<T>> repo,
            IEnumerable<IDomainEventHandler<IDomain<T>>> eventHandlers,
            Lazy<IQueryableRepository<Transaction>> transactionRepo,
            Lazy<IQueryableRepository<Accounting>> accountingRepo,
            Lazy<IEnumerable<IAccountingAuditor>> auditors,
            Lazy<ICacheStore> cacheStore,
            ILogger logger)
            : base(repo, eventHandlers)
        {
            this.transactionRepo = transactionRepo;
            this.accountingRepo = accountingRepo;
            this.auditors = auditors;
            this.cacheStore = cacheStore;
            this.logger = logger;
        }

        public abstract string AccountingPrincipal { get; }

        public async Task MakeAccountingAsync()
        {
            //REMARK (5he11) 取以当前时间为基础的昨天的最后一刻并转化为UTC时间，规范后的值如：2018-08-08 23:59:59.999 +00:00
            DateTimeOffset targetTime = new DateTimeOffset(DateTimeOffset.UtcNow.UtcDateTime.Date.ToUniversalTime()).AddMilliseconds(-1);
            Accounting latest = await GetLatestAccountingAsync(targetTime);
            if (latest.ETag == null)
            {
                //REMARK (5he11) ETag为空指示系统中未找到任何数据，即新账号，仅生成昨天的记录即可
                await MakeAccountingAsync(targetTime, 0);
            }
            else
            {
                DateTimeOffset pos = latest.GetEnd();
                long previousBalance = latest.Balance;
                while (pos < targetTime)
                {
                    //REMARK (5he11) 数据层存储的都是记账日当天最后一刻的时间，所以加1毫秒就是新的要创建账务的日期
                    DateTimeOffset buildTarget = pos.AddMilliseconds(1);
                    Accounting accounting = await MakeAccountingAsync(buildTarget, previousBalance);
                    if (accounting == null)
                    {
                        break;
                    }
                    previousBalance = accounting.Balance;
                    pos = pos.AddDays(1);
                }
            }
        }

        public IAsyncEnumerable<Transaction> GetTransactionsAsync(DateTimeOffset fromInclusive, DateTimeOffset toInclusive)
        {
            string principal = AccountingPrincipal;
            return transactionRepo.Value.QueryAsync(
                      Transaction.BuildPartitionKey(principal),
                      Transaction.BuildRowKey(toInclusive),
                      Transaction.BuildRowKey(fromInclusive));
        }

        public async Task<double> GetFrozenAsync()
        {
            string pk = FrozenKey;
            string rk = AccountingPrincipal;
            double result = await cacheStore.Value.GetAsync<double>(pk, rk);
            return result;
        }

        public async Task<double> FreezeAsync(double amount)
        {
            amount = amount.ChineseRound();
            if (amount < 0)
            {
                throw new ArgumentException("金额不应为负值", nameof(amount));
            }
            string pk = FrozenKey;
            string rk = AccountingPrincipal;
            double currentValue = await cacheStore.Value.GetAsync<double>(pk, rk);
            double result = currentValue + amount;
            await cacheStore.Value.SetAsync(pk, rk, result, false);
            return result;
        }

        public async Task<double> UnfreezeAsync()
        {
            string pk = FrozenKey;
            string rk = AccountingPrincipal;
            await cacheStore.Value.DeleteAsync(pk, rk);
            return 0;
        }

        public async Task<double> UnfreezeAsync(double amount)
        {
            amount = amount.ChineseRound();
            if (amount < 0)
            {
                throw new ArgumentException("金额不应为负值", nameof(amount));
            }
            string pk = FrozenKey;
            string rk = AccountingPrincipal;
            double currentValue = await cacheStore.Value.GetAsync<double>(pk, rk);
            double result = currentValue - amount;
            await cacheStore.Value.SetAsync(pk, rk, result, false);
            return result;
        }

        public Task<TransactionRequest> BuildTransactionAsync(
            long delta, int reason, string remark, string reference, string id = null, string corelation = null)
        {
            return Task.FromResult(new TransactionRequest(AccountingPrincipal, delta)
            {
                Reason = reason,
                ID = id,
                Reference = reference,
                Remark = remark,
                Corelation = corelation,
            });
        }

        public async Task<IEnumerable<Transaction>> MakeTransactionAsync(
            long delta, int reason, string remark, string reference, string id = null, string corelation = null)
        {
            return await MakeTransactionAsync(new[] { await BuildTransactionAsync(delta, reason, remark, reference, id, corelation) });
        }

        public async Task<IEnumerable<Transaction>> MakeTransactionAsync(TransactionRequest request)
        {
            return await MakeTransactionAsync(new[] { request });
        }

        //TODO (5he11) 此方法要加锁并且实现事务
        public async Task<IEnumerable<Transaction>> MakeTransactionAsync(IEnumerable<TransactionRequest> requests)
        {
            List<Transaction> transactions = new();
            int count = 0;
            foreach (TransactionRequest request in requests)
            {
                if (string.IsNullOrWhiteSpace(request.Target))
                {
                    throw new ArgumentNullException(nameof(requests));
                }

                request.ID ??= Transaction.BuildRowKey(DateTimeOffset.UtcNow);
                request.Target = request.Target.Trim();

                Transaction transaction = new()
                {
                    Delta = request.Delta,
                    Remark = request.Remark,
                    Reason = request.Reason,
                    Reference = request.Reference,
                    Corelation = request.Corelation,
                };
                transaction.SetOwner(request.Target);
                transaction.RowKey = request.ID;
                transactions.Add(transaction);
                count++;
            }

            await transactionRepo.Value.CreateAsync(transactions);
            string now = DateTimeOffset.UtcNow.ToSixDigitsDate();
            foreach (Transaction transaction in transactions)
            {
                string pk = $"{DeltaKey}-{now}";
                string rk = transaction.GetOwner();
                long currentValue = await cacheStore.Value.GetAsync<long>(pk, rk);
                long result = currentValue + transaction.Delta;
                await cacheStore.Value.SetAsync(pk, rk, result, false);
            }
            return transactions;
        }

        public async Task<Transaction> GetTransactionAsync(DateTimeOffset id)
        {
            string target = AccountingPrincipal;
            Transaction transaction = await transactionRepo.Value.RetrieveAsync(target, Transaction.BuildRowKey(id));
            return transaction;
        }

        public async Task<AccountBalance> GetBalanceAsync(DateTimeOffset input)
        {
            //REMARK (5he11) 将输入限制为仅取其日期的当日的最后一刻并转化为UTC时间，规范后的值如：2018-08-08 23:59:59.999 +00:00
            input = new DateTimeOffset(input.UtcDateTime.Date.ToUniversalTime()).AddDays(1).AddMilliseconds(-1);
            double frozen = await GetFrozenAsync();
            frozen = frozen.ChineseRound();
            bool queryCache;
            DateTimeOffset lastAccountDate = input;
            if (input.UtcDateTime.Date.ToUniversalTime() != DateTimeOffset.UtcNow.UtcDateTime.Date.ToUniversalTime())
            {
                //REMARK (5he11) 提高执行效率，不查询当天的值可以尝试是否可以直接命中
                queryCache = false;
            }
            else
            {
                lastAccountDate = lastAccountDate.AddDays(-1);
                //REMARK (5he11) 提高执行效率，查询当天的值时，首先尝试直接命中前一天晚上临结束时的准确账目
                queryCache = true;
            }

            double balance;
            string principal = AccountingPrincipal;
            Accounting accounting = await accountingRepo.Value.RetrieveAsync(Accounting.BuildPartitionKey(principal), Accounting.BuildRowKey(lastAccountDate));
            if (accounting == null)
            {
                //REMARK (5he11) 若无法高效命中则使用慢速范围查询
                accounting = await GetLatestAccountingAsync(lastAccountDate);
                queryCache = true;
            }
            balance = accounting.Balance;

            if (queryCache)
            {
                //REMARK (5he11) 增加1毫秒使起始值规范为第2天的0点，以此避免将以日期命名的缓存多加一遍
                //REMARK (5he11) 但如果结果的 ETag 为空则表示是新用户，需累加其所有缓存的值即可，这里的做法是累加3天，而且输入时间减去1毫秒让循环至少执行一次
                DateTimeOffset pos = accounting.ETag == null ? input.AddMilliseconds(-1).AddDays(-3) : accounting.GetEnd().AddMilliseconds(1);
                while (pos < input)
                {
                    double delta = await GetDeltaAsync(pos);
                    balance += delta;
                    pos = pos.AddDays(1);
                }
            }

            balance = balance.ChineseRound();

            return new AccountBalance
            {
                Total = balance,
                Frozen = frozen,
                Available = balance - frozen
            };
        }

        private async Task<long> GetDeltaAsync(DateTimeOffset input)
        {
            string pk = $"{DeltaKey}-{input.ToSixDigitsDate()}";
            string rk = AccountingPrincipal;
            return await cacheStore.Value.GetAsync<long>(pk, rk);
        }

        private async Task ClearDeltaAsync(DateTimeOffset input)
        {
            string pk = $"{DeltaKey}-{input.ToSixDigitsDate()}";
            string rk = AccountingPrincipal;
            await cacheStore.Value.DeleteAsync(pk, rk);
        }

        private async Task<Accounting> MakeAccountingAsync(DateTimeOffset input, long previousBalance)
        {
            //REMARK (5he11) 将输入限制为仅取其日期的当日的最后一刻并转化为UTC时间，规范后的值如：2018-08-08 23:59:59.999 +00:00
            input = new DateTimeOffset(input.UtcDateTime.Date.ToUniversalTime()).AddDays(1).AddMilliseconds(-1);
            if (input > DateTimeOffset.UtcNow)
            {
                //REMARK (5he11) 生成账务的时间不应该比当前时间靠后，否则会造成账务错误
                return null;
            }

            //REMARK (5he11) 取其日期的当日的第一刻并转化为UTC时间，规范后的值如：2018-08-08 00:00:00.000 +00:00
            DateTimeOffset transactionSearchFrom = new(input.UtcDateTime.Date.ToUniversalTime());
            List<Transaction> transactions = await GetTransactionsAsync(transactionSearchFrom, input).ToListAsync();

            long credits = transactions.Where(t => t.Delta > 0).Select(t => t.Delta).DefaultIfEmpty().Sum();
            long debits = transactions.Where(t => t.Delta < 0).Select(t => t.Delta).DefaultIfEmpty().Sum();
            long delta = await GetDeltaAsync(input);
            long diff = credits + debits - delta;
            long b = previousBalance + credits + debits;
            string principal = AccountingPrincipal;
            logger.LogInformation($"账务主体 {principal} 从 {transactionSearchFrom} 开始，截止到 {input} 为止的账务变化为 {credits}/{debits} 与该日缓存相差 {diff} 截止此时余额为 {b}");

            //if (Math.Abs(diff) > 1)
            //{
            //    //TODO (5he11) 发邮件报警
            //    return null;
            //}

            Accounting accounting = new()
            {
                Balance = previousBalance + credits + debits,
                Credits = credits,
                Debits = debits,
            };
            accounting.SetPrincipal(principal);
            accounting.SetEnd(input);

            foreach (IAccountingAuditor auditor in auditors.Value)
            {
                await auditor.AuditAsync(accounting, transactions);
            }

            await accountingRepo.Value.CreateAsync(accounting);
            await ClearDeltaAsync(input);
            return accounting;
        }

        private async Task<Accounting> GetLatestAccountingAsync(DateTimeOffset input)
        {
            //REMARK (5he11) 将输入限制为仅取其日期的当日的最后一刻并转化为UTC时间，规范后的值如：2018-08-08 23:59:59.999 +00:00
            input = new DateTimeOffset(input.UtcDateTime.Date.ToUniversalTime()).AddDays(1).AddMilliseconds(-1);
            string principal = AccountingPrincipal;
            DateTimeOffset searchFrom = input.AddDays(-30);
            List<Accounting> accountings = await accountingRepo.Value.QueryAsync(
                                    Accounting.BuildPartitionKey(principal),
                                    Accounting.BuildRowKey(input),
                                    Accounting.BuildRowKey(searchFrom))
                                .ToListAsync();
            Accounting latest = accountings.OrderByDescending(a => a.GetEnd()).FirstOrDefault();
            if (latest != null)
            {
                return latest;
            }
            Accounting empty = new()
            {
                Balance = 0,
                Credits = 0,
                Debits = 0
            };
            empty.SetEnd(input);
            empty.SetPrincipal(principal);
            return empty;
        }
    }
}