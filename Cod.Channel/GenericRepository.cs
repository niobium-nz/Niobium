using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Cod.Channel
{
    public class GenericRepository<TDomain, TEntity> : IRepository<TDomain, TEntity>
        where TEntity : IEntity
        where TDomain : IChannelDomain<TEntity>
    {
        private readonly IConfigurationProvider configuration;
        private readonly HttpClient httpClient;
        private readonly IAuthenticator authenticator;
        private readonly Func<TDomain> createDomain;
        private readonly Dictionary<TableStorageFetchKey, ContinuationToken> fetchHistory;

        public GenericRepository(IConfigurationProvider configuration, HttpClient httpClient,
            IAuthenticator authenticator, Func<TDomain> createDomain)
        {
            this.fetchHistory = new Dictionary<TableStorageFetchKey, ContinuationToken>();
            this.CachedData = new List<TDomain>();
            this.configuration = configuration;
            this.httpClient = httpClient;
            this.authenticator = authenticator;
            this.createDomain = createDomain;
        }

        protected List<TDomain> CachedData { get; private set; }

        public virtual IReadOnlyCollection<TDomain> Data => this.CachedData;

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

        public async Task<OperationResult<IReadOnlyCollection<TDomain>>> LoadAsync(string partitionKey, int count = -1, bool force = false, bool continueToLoadMore = false)
            => await this.LoadAsync(partitionKey, partitionKey, null, null, count, force, continueToLoadMore);

        public async Task<OperationResult<IReadOnlyCollection<TDomain>>> LoadAsync(string partitionKey, string rowKeyStart, string rowKeyEnd, int count = -1, bool force = false, bool continueToLoadMore = false)
            => await this.LoadAsync(partitionKey, partitionKey, rowKeyStart, rowKeyEnd, count, force, continueToLoadMore);

        public async Task<OperationResult<IReadOnlyCollection<TDomain>>> LoadAsync(string partitionKeyStart, string partitionKeyEnd, int count = -1, bool force = false, bool continueToLoadMore = false)
            => await this.LoadAsync(partitionKeyStart, partitionKeyEnd, null, null, count, force, continueToLoadMore);

        public async Task<OperationResult<IReadOnlyCollection<TDomain>>> LoadAsync(string partitionKeyStart, string partitionKeyEnd, string rowKeyStart, string rowKeyEnd, int count = -1, bool force = false, bool continueToLoadMore = false)
        {
            IReadOnlyCollection<TDomain> result = new List<TDomain>();
            var key = new TableStorageFetchKey
            {
                Count = count,
                PartitionKeyEnd = partitionKeyEnd,
                PartitionKeyStart = partitionKeyStart,
                RowKeyEnd = rowKeyEnd,
                RowKeyStart = rowKeyStart,
            };

            if (!force
                && partitionKeyStart != null && rowKeyStart != null
                && partitionKeyStart == partitionKeyEnd && rowKeyStart == rowKeyEnd)
            {
                // REMARK (5he11) 这种情况下是查询一个准确命中的数据，并且不要求强制刷新数据，所以应该看一下数据是否有缓存，有则跳过网络请求
                var cache = this.CachedData.SingleOrDefault(c => c.PartitionKey == partitionKeyStart && c.RowKey == rowKeyStart);

                if (cache != null)
                {
                    return OperationResult<IReadOnlyCollection<TDomain>>.Create(new[] { cache });
                }
            }

            var proceed = false;
            ContinuationToken continuationToken = null;
            if (force || !fetchHistory.ContainsKey(key))
            {
                if (force && continueToLoadMore)
                {
                    throw new NotSupportedException($"{nameof(force)} and {nameof(continueToLoadMore)} cannot be both true.");
                }

                proceed = true;
            }
            else if (fetchHistory.ContainsKey(key))
            {
                if (continueToLoadMore)
                {
                    continuationToken = fetchHistory[key];
                    proceed = true;
                }
            }
            else
            {
                throw new NotImplementedException("TODO");
            }

            if (proceed)
            {
                var response = await this.FetchAsync(partitionKeyStart, partitionKeyEnd, rowKeyStart, rowKeyEnd, count, continuationToken);
                if (response.IsSuccess)
                {
                    if (this.fetchHistory.ContainsKey(key))
                    {
                        this.fetchHistory.Remove(key);
                    }
                    this.fetchHistory.Add(key, response.Result.ContinuationToken);

                    if (response.Result.Data.Count > 0)
                    {
                        result = this.Cache(response.Result.Data);
                    }
                }
                else
                {
                    return new OperationResult<IReadOnlyCollection<TDomain>>(response.Code, null)
                    {
                        Message = response.Message,
                        Reference = response.Reference,
                    };
                }
            }

            return OperationResult<IReadOnlyCollection<TDomain>>.Create(result);
        }

        public async Task<OperationResult<IReadOnlyCollection<TDomain>>> LoadAsync(int count = -1, bool force = false, bool continueToLoadMore = false)
            => await this.LoadAsync(null, null, null, null, count, force, continueToLoadMore);

        protected virtual async Task<OperationResult<TableQueryResult<TEntity>>> FetchAsync(string partitionKeyStart, string partitionKeyEnd, string rowKeyStart, string rowKeyEnd, int count, ContinuationToken continuationToken)
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
                if (partitionKeyStart == partitionKeyEnd.Substring(0, partitionKeyEnd.Length - 1)
                    && partitionKeyEnd.EndsWith("~"))
                {
                    pk = partitionKeyEnd;
                }
                else
                {
                    throw new NotImplementedException("TODO: querying on special partition key range has not yet implemented.");
                }
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

            var baseUrl = await this.configuration.GetSettingAsStringAsync(Constants.KEY_TABLE_URL);
            return await TableStorageHelper.GetAsync<TEntity>(this.httpClient, baseUrl, signature.Result.Signature, partitionKeyStart, partitionKeyEnd, rowKeyStart, rowKeyEnd, continuationToken, count);
        }

        protected virtual IReadOnlyCollection<TDomain> Cache(IEnumerable<TEntity> entities)
        {
            var result = new List<TDomain>();

            foreach (var entity in entities)
            {
                var existing = this.CachedData.SingleOrDefault(d => d.PartitionKey == entity.PartitionKey && d.RowKey == entity.RowKey);
                if (existing != null)
                {
                    if (existing.Entity.ETag != entity.ETag)
                    {
                        var i = this.CachedData.IndexOf(existing);
                        var d = this.ToDomain(entity);
                        this.CachedData[i] = d;
                        result.Add(d);
                    }
                }
                else
                {
                    var d = this.ToDomain(entity);
                    this.CachedData.Add(d);
                    result.Add(d);
                }
            }

            return result;
        }

        protected virtual void Uncache(TDomain domainObject) => Uncache(new[] { domainObject });

        protected virtual void Uncache(IEnumerable<TDomain> domainObjects)
            => this.CachedData.RemoveAll(c => domainObjects.Any(dobj => dobj.PartitionKey == c.PartitionKey && dobj.RowKey == c.RowKey));

        protected virtual void Uncache(TEntity entity) => Uncache(new[] { entity });

        protected virtual void Uncache(IEnumerable<TEntity> entities)
            => this.CachedData.RemoveAll(c => entities.Any(en => en.PartitionKey == c.PartitionKey && en.RowKey == c.RowKey));

        protected virtual TDomain ToDomain(TEntity entity) => (TDomain)this.createDomain().Initialize(entity);

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
