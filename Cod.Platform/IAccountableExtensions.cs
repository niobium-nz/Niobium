using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cod.Platform.Model;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Table;

namespace Cod.Platform
{
    public static class IAccountableExtensions
    {
        private const string FROZEN_KEY = "frozen";
        private const string DELTA_KEY = "delta";

        public static async Task MakeAccountingAsync(this IAccountable accountable, IEnumerable<IAccountingAuditor> auditors)
        {
            //REMARK (5he11) 取以当前时间为基础的昨天的最后一刻并转化为UTC时间，规范后的值如：2018-08-08 23:59:59.999 +00:00
            var targetTime = new DateTimeOffset(DateTimeOffset.UtcNow.UtcDateTime.Date.ToUniversalTime()).AddMilliseconds(-1);
            var latest = await accountable.GetLatestAccountingAsync(targetTime);
            if (latest.ETag == null)
            {
                //REMARK (5he11) ETag为空指示系统中未找到任何数据，即新账号，仅生成昨天的记录即可
                await accountable.MakeAccountingAsync(targetTime, 0, auditors);
            }
            else
            {
                var pos = latest.GetEnd();
                var previousBalance = latest.Balance;
                while (pos < targetTime)
                {
                    //REMARK (5he11) 数据层存储的都是记账日当天最后一刻的时间，所以加1毫秒就是新的要创建账务的日期
                    var buildTarget = pos.AddMilliseconds(1);
                    var accounting = await accountable.MakeAccountingAsync(buildTarget, previousBalance, auditors);
                    if (accounting == null)
                    {
                        break;
                    }
                    previousBalance = accounting.Balance;
                    pos = pos.AddDays(1);
                }
            }
        }

        public static async Task<IEnumerable<Transaction>> GetTransactionsAsync(this IAccountable accountable, DateTimeOffset fromInclusive, DateTimeOffset toInclusive)
          => await CloudStorage.GetTable<Transaction>().WhereAsync<Transaction>(TableQuery.CombineFilters(
                TableQuery.GenerateFilterCondition(nameof(Transaction.PartitionKey), QueryComparisons.Equal, Transaction.BuildPartitionKey(await accountable.GetAccountingPrincipalAsync())),
                TableOperators.And,
                TableQuery.CombineFilters(TableQuery.GenerateFilterCondition(nameof(Transaction.RowKey), QueryComparisons.LessThanOrEqual, Transaction.BuildRowKey(fromInclusive)),
                TableOperators.And,
                TableQuery.GenerateFilterCondition(nameof(Transaction.RowKey), QueryComparisons.GreaterThanOrEqual, Transaction.BuildRowKey(toInclusive)))));

        public static async Task<double> GetFrozenAsync(this IAccountable accountable)
        {
            var pk = FROZEN_KEY;
            var rk = await accountable.GetAccountingPrincipalAsync();
            var result = await accountable.CacheStore.GetAsync<double>(pk, rk);
            return result;
        }

        public static async Task<double> FreezeAsync(this IAccountable accountable, double amount)
        {
            amount = Math.Abs(amount.ChineseRound());
            var pk = FROZEN_KEY;
            var rk = await accountable.GetAccountingPrincipalAsync();
            var currentValue = await accountable.CacheStore.GetAsync<double>(pk, rk);
            var result = currentValue - amount;
            await accountable.CacheStore.SetAsync(pk, rk, result, false);
            return result;
        }

        public static async Task<double> UnfreezeAsync(this IAccountable accountable)
        {
            var pk = FROZEN_KEY;
            var rk = await accountable.GetAccountingPrincipalAsync();
            await accountable.CacheStore.DeleteAsync(pk, rk);
            return 0;
        }

        public static async Task<double> UnfreezeAsync(this IAccountable accountable, double amount)
            => await FreezeAsync(accountable, -amount);

        public static async Task<double> GetDeltaAsync(this IAccountable accountable, DateTimeOffset input)
        {
            var pk = $"{DELTA_KEY}-{input.ToSixDigitsDate()}";
            var rk = await accountable.GetAccountingPrincipalAsync();
            var result = await accountable.CacheStore.GetAsync<double>(pk, rk);
            return result;
        }

        public static async Task ClearDeltaAsync(this IAccountable accountable, DateTimeOffset input)
        {
            var pk = $"{DELTA_KEY}-{input.ToSixDigitsDate()}";
            var rk = await accountable.GetAccountingPrincipalAsync();
            await accountable.CacheStore.DeleteAsync(pk, rk);
        }

        public static async Task<TransactionRequest> BuildTransactionAsync(this IAccountable accountable,
            double delta, int reason, string remark, string reference, DateTimeOffset? id = null)
            => new TransactionRequest(await accountable.GetAccountingPrincipalAsync(), delta)
            {
                Reason = reason,
                ID = id,
                Reference = reference,
                Remark = remark,
            };

        public static async Task<IEnumerable<Transaction>> MakeTransactionAsync(this IAccountable accountable,
            double delta, int reason, string remark, string reference, DateTimeOffset? id = null)
            => await MakeTransactionAsync(new[] { await accountable.BuildTransactionAsync(delta, reason, remark, reference, id) }, accountable.CacheStore);

        public static async Task<IEnumerable<Transaction>> MakeTransactionAsync(TransactionRequest request, ICacheStore cacheStore)
          => await MakeTransactionAsync(new[] { request }, cacheStore);

        //TODO (5he11) 此方法要加锁并且实现事务
        public static async Task<IEnumerable<Transaction>> MakeTransactionAsync(IEnumerable<TransactionRequest> requests, ICacheStore cacheStore)
        {
            var transactions = new List<Transaction>();
            var count = 0;
            foreach (var request in requests)
            {
                if (String.IsNullOrWhiteSpace(request.Target))
                {
                    throw new ArgumentNullException(nameof(request.Target));
                }

                if (!request.ID.HasValue)
                {
                    request.ID = DateTimeOffset.UtcNow;
                }
                request.Delta = request.Delta.ChineseRound();
                request.Target = request.Target.Trim();

                var transaction = new Transaction
                {
                    Delta = request.Delta,
                    Remark = request.Remark,
                    Reason = request.Reason,
                    Reference = request.Reference
                };
                transaction.SetOwner(request.Target);
                var id = request.ID.Value.AddMilliseconds(count);
                transaction.SetCreated(id);
                transactions.Add(transaction);
                count++;
            }

            await CloudStorage.GetTable<Transaction>().InsertAsync(transactions);
            var now = DateTimeOffset.UtcNow.ToSixDigitsDate();
            foreach (var transaction in transactions)
            {
                var pk = $"{DELTA_KEY}-{now}";
                var rk = transaction.GetOwner();
                var currentValue = await cacheStore.GetAsync<double>(pk, rk);
                var result = currentValue + transaction.Delta;
                await cacheStore.SetAsync(pk, rk, result, false);
            }
            return transactions;
        }

        public static async Task<AccountBalance> GetBalanceAsync(this IAccountable accountable, DateTimeOffset input)
        {
            //REMARK (5he11) 将输入限制为仅取其日期的当日的最后一刻并转化为UTC时间，规范后的值如：2018-08-08 23:59:59.999 +00:00
            input = new DateTimeOffset(input.UtcDateTime.Date.ToUniversalTime()).AddDays(1).AddMilliseconds(-1);
            var frozen = await accountable.GetFrozenAsync();
            Accounting accounting = null;
            if (input.UtcDateTime.Date.ToUniversalTime() != DateTimeOffset.UtcNow.UtcDateTime.Date.ToUniversalTime())
            {
                //REMARK (5he11)  提高执行效率，不查询当天的值可以尝试是否可以直接命中
                var principal = await accountable.GetAccountingPrincipalAsync();
                accounting = await CloudStorage.GetTable<Accounting>().RetrieveAsync<Accounting>(
                    Accounting.BuildPartitionKey(principal), Accounting.BuildRowKey(input));
            }

            double balance;
            if (accounting != null)
            {
                balance = accounting.Balance;
            }
            else
            {
                accounting = await accountable.GetLatestAccountingAsync(input);
                balance = accounting.Balance;

                //REMARK (5he11) 增加1毫秒使起始值规范为第2天的0点，以此避免将以日期命名的缓存多加一遍
                //REMARK (5he11) 但如果结果的 ETag 为空则表示是新用户，需累加其所有缓存的值即可，这里的做法是累加3天，而且输入时间减去1毫秒让循环至少执行一次
                var pos = accounting.ETag == null ? input.AddMilliseconds(-1).AddDays(-3) : accounting.GetEnd().AddMilliseconds(1);
                while (pos < input)
                {
                    var delta = await accountable.GetDeltaAsync(pos);
                    balance += delta;
                    pos = pos.AddDays(1);
                }
            }
            return new AccountBalance
            {
                Total = balance,
                Frozen = frozen,
                Available = balance + frozen
            };
        }

        private static async Task<Accounting> MakeAccountingAsync(this IAccountable accountable, DateTimeOffset input, double previousBalance,
            IEnumerable<IAccountingAuditor> auditors)
        {
            //REMARK (5he11) 将输入限制为仅取其日期的当日的最后一刻并转化为UTC时间，规范后的值如：2018-08-08 23:59:59.999 +00:00
            input = new DateTimeOffset(input.UtcDateTime.Date.ToUniversalTime()).AddDays(1).AddMilliseconds(-1);
            if (input > DateTimeOffset.UtcNow)
            {
                //REMARK (5he11) 生成账务的时间不应该比当前时间靠后，否则会造成账务错误
                return null;
            }

            //REMARK (5he11) 取其日期的当日的第一刻并转化为UTC时间，规范后的值如：2018-08-08 00:00:00.000 +00:00
            var transactionSearchFrom = new DateTimeOffset(input.UtcDateTime.Date.ToUniversalTime());
            var transactions = await accountable.GetTransactionsAsync(transactionSearchFrom, input);
            var credits = transactions.Where(t => t.Delta > 0).Select(t => t.Delta).DefaultIfEmpty().Sum();
            var debits = transactions.Where(t => t.Delta < 0).Select(t => t.Delta).DefaultIfEmpty().Sum();
            var delta = await accountable.GetDeltaAsync(input);
            var diff = credits + debits - delta;
            var b = (previousBalance + credits + debits).ChineseRound();
            var principal = await accountable.GetAccountingPrincipalAsync();
            if (accountable is ILoggerSite ls)
            {
                ls.Logger.LogInformation($"账务主体 {principal} 从 {transactionSearchFrom} 开始，截止到 {input} 为止的账务变化为 {credits}/{debits} 与该日缓存相差 {diff} 截止此时余额为 {b}");
            }

            //if (Math.Abs(diff) > 1)
            //{
            //    //TODO (5he11) 发邮件报警
            //    return null;
            //}

            var accounting = new Accounting
            {
                Balance = (previousBalance + credits + debits).ChineseRound(),
                Credits = credits.ChineseRound(),
                Debits = debits.ChineseRound()
            };
            accounting.SetPrincipal(principal);
            accounting.SetEnd(input);

            if (auditors != null)
            {
                foreach (var auditor in auditors)
                {
                    await auditor.AuditAsync(accounting, transactions);
                }
            }

            await CloudStorage.GetTable<Accounting>().InsertAsync(new[] { accounting });
            await accountable.ClearDeltaAsync(input);
            return accounting;
        }

        private static async Task<Accounting> GetLatestAccountingAsync(this IAccountable accountable, DateTimeOffset input)
        {
            //REMARK (5he11) 将输入限制为仅取其日期的当日的最后一刻并转化为UTC时间，规范后的值如：2018-08-08 23:59:59.999 +00:00
            input = new DateTimeOffset(input.UtcDateTime.Date.ToUniversalTime()).AddDays(1).AddMilliseconds(-1);
            var principal = await accountable.GetAccountingPrincipalAsync();
            var searchFrom = input.AddDays(-30);
            var accountings = await CloudStorage.GetTable<Accounting>().WhereAsync<Accounting>(TableQuery.CombineFilters(
                TableQuery.GenerateFilterCondition(nameof(Accounting.PartitionKey), QueryComparisons.Equal, Accounting.BuildPartitionKey(principal)),
                TableOperators.And,
                TableQuery.CombineFilters(
                TableQuery.GenerateFilterCondition(nameof(Accounting.RowKey), QueryComparisons.LessThanOrEqual, Accounting.BuildRowKey(searchFrom)),
                TableOperators.And,
                TableQuery.GenerateFilterCondition(nameof(Accounting.RowKey), QueryComparisons.GreaterThanOrEqual, Accounting.BuildRowKey(input)))));
            var latest = accountings.OrderByDescending(a => a.GetEnd()).FirstOrDefault();
            if (latest != null)
            {
                return latest;
            }
            var empty = new Accounting
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
