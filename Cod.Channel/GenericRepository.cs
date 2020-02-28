using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Cod.Channel
{
    public class GenericRepository<TDomain, TEntity> : IRepository<TDomain, TEntity>
        where TDomain : IChannelDomain<TEntity>
    {
        private readonly IConfigurationProvider configuration;
        private readonly HttpClient httpClient;
        private readonly IAuthenticator authenticator;
        private readonly Func<TDomain> createDomain;
        private readonly List<TDomain> cache;
        private readonly Dictionary<TableStorageFetchKey, ContinuationToken> fetchHistory;

        public GenericRepository(IConfigurationProvider configuration, HttpClient httpClient,
            IAuthenticator authenticator, Func<TDomain> createDomain)
        {
            this.fetchHistory = new Dictionary<TableStorageFetchKey, ContinuationToken>();
            this.cache = new List<TDomain>();
            this.configuration = configuration;
            this.httpClient = httpClient;
            this.authenticator = authenticator;
            this.createDomain = createDomain;
        }

        public IReadOnlyCollection<TDomain> Data => this.cache;

        public async Task<OperationResult<TDomain>> LoadAsync(string partitionKey, string rowKey, bool force = false)
        {
            var result = await this.LoadAsync(partitionKey, partitionKey, rowKey, rowKey, 1, force);
            if (result.IsSuccess)
            {
                return OperationResult<TDomain>.Create(this.Data.SingleOrDefault(c => c.PartitionKey == partitionKey && c.RowKey == rowKey));
            }
            return new OperationResult<TDomain>(result.Code, default)
            {
                Message = result.Message,
                Reference = result.Reference
            };
        }

        public async Task<OperationResult<ContinuationToken>> LoadAsync(string partitionKey, int count = -1, bool force = false)
            => await this.LoadAsync(partitionKey, partitionKey, null, null, count, force);

        public async Task<OperationResult<ContinuationToken>> LoadAsync(string partitionKey, string rowKeyStart, string rowKeyEnd, int count = -1, bool force = false)
            => await this.LoadAsync(partitionKey, partitionKey, rowKeyStart, rowKeyEnd, count, force);

        public async Task<OperationResult<ContinuationToken>> LoadAsync(string partitionKeyStart, string partitionKeyEnd, int count = -1, bool force = false)
            => await this.LoadAsync(partitionKeyStart, partitionKeyEnd, null, null, count, force);

        public async Task<OperationResult<ContinuationToken>> LoadAsync(string partitionKeyStart, string partitionKeyEnd, string rowKeyStart, string rowKeyEnd, int count = -1, bool force = false)
        {
            ContinuationToken result;
            var key = new TableStorageFetchKey
            {
                Count = count,
                PartitionKeyEnd = partitionKeyEnd,
                PartitionKeyStart = partitionKeyStart,
                RowKeyEnd = rowKeyEnd,
                RowKeyStart = rowKeyStart,
            };

            if (force || !fetchHistory.ContainsKey(key))
            {
                var response = await this.FetchAsync(partitionKeyStart, partitionKeyEnd, rowKeyStart, rowKeyEnd, count);
                if (response.IsSuccess)
                {
                    result = response.Result.ContinuationToken;
                    if (!this.fetchHistory.ContainsKey(key))
                    {
                        this.fetchHistory.Add(key, result);
                    }
                    
                    if (response.Result.Data.Count > 0)
                    {
                        var domainObjects = response.Result.Data.Select(m => (TDomain)this.createDomain().Initialize(m));
                        this.AddToCache(domainObjects);
                    }
                }
                else
                {
                    return new OperationResult<ContinuationToken>(response.Code, null)
                    {
                        Message = response.Message,
                        Reference = response.Reference,
                    };
                }
            }
            else if (fetchHistory.ContainsKey(key))
            {
                result = fetchHistory[key];
            }
            else
            {
                throw new NotImplementedException("TODO");
            }

            return OperationResult<ContinuationToken>.Create(result);
        }

        public async Task<OperationResult<ContinuationToken>> LoadAsync(int count = -1, bool force = false)
            => await this.LoadAsync(null, null, null, null, count, force);

        protected virtual async Task<OperationResult<TableQueryResult<TEntity>>> FetchAsync(string partitionKeyStart, string partitionKeyEnd, string rowKeyStart, string rowKeyEnd, int count)
        {
            string pk, rk;
            if (partitionKeyStart == null && partitionKeyEnd == null)
            {
                pk = null;
            }
            else if (partitionKeyStart == partitionKeyEnd)
            {
                pk = partitionKeyStart;
            }
            else if (partitionKeyStart != null && partitionKeyEnd != null && partitionKeyStart != partitionKeyEnd)
            {
                throw new NotImplementedException("TODO: querying on partition key range has not yet implemented.");
            }
            else
            {
                throw new NotSupportedException();
            }

            if (rowKeyStart == null && rowKeyEnd == null)
            {
                rk = null;
            }
            else if (rowKeyStart == rowKeyEnd)
            {
                rk = rowKeyStart;
            }
            else if (rowKeyStart != null && rowKeyEnd != null && rowKeyStart != rowKeyEnd)
            {
                // REMARK (5he11) 这种情况仅在签名上请求PK，而在查询过滤条件上过滤RK
                rk = null;
            }
            else
            {
                throw new NotSupportedException();
            }


            var signature = await this.authenticator.AquireSignatureAsync(StorageType.Table, typeof(TEntity).Name, pk, rk);
            if (!signature.IsSuccess)
            {
                return new OperationResult<TableQueryResult<TEntity>>(signature.Code, null)
                {
                    Message = signature.Message,
                    Reference = signature.Reference,
                };
            }

            var baseUrl = await this.configuration.GetSettingAsync(Constants.KEY_TABLE_URL);
            return await TableStorageHelper.GetAsync<TEntity>(this.httpClient, baseUrl, signature.Result.Signature, partitionKeyStart, partitionKeyEnd, rowKeyStart, rowKeyEnd, null, count);
        }

        protected virtual void AddToCache(TDomain domainObject) => AddToCache(new[] { domainObject });

        protected virtual void AddToCache(IEnumerable<TDomain> domainObjects)
        {
            this.cache.RemoveAll(c => domainObjects.Any(dobj => dobj.PartitionKey == c.PartitionKey && dobj.RowKey == c.RowKey));
            this.cache.AddRange(domainObjects);
        }

        protected struct TableStorageFetchKey : IEquatable<TableStorageFetchKey>
        {
            public string PartitionKeyStart { get; set; }

            public string PartitionKeyEnd { get; set; }

            public string RowKeyStart { get; set; }

            public string RowKeyEnd { get; set; }

            public int Count { get; set; }

            public override bool Equals(object obj) => obj is TableStorageFetchKey key && this.Equals(key);

            public bool Equals(TableStorageFetchKey other) => this.PartitionKeyStart == other.PartitionKeyStart && this.PartitionKeyEnd == other.PartitionKeyEnd && this.RowKeyStart == other.RowKeyStart && this.RowKeyEnd == other.RowKeyEnd && this.Count == other.Count;

            public override int GetHashCode()
            {
                var hashCode = -1203710578;
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(this.PartitionKeyStart);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(this.PartitionKeyEnd);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(this.RowKeyStart);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(this.RowKeyEnd);
                hashCode = hashCode * -1521134295 + this.Count.GetHashCode();
                return hashCode;
            }

            public static bool operator ==(TableStorageFetchKey left, TableStorageFetchKey right) => left.Equals(right);

            public static bool operator !=(TableStorageFetchKey left, TableStorageFetchKey right) => !(left == right);
        }
    }
}
