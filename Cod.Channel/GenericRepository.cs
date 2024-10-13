//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;

//namespace Cod.Channel
//{
//    public class GenericRepository<TDomain, TEntity> : IRepository<TDomain, TEntity>
//        where TEntity : class, new()
//        where TDomain : IDomain<TEntity>
//    {
//        private readonly IConfigurationProvider configuration;
//        private readonly IHttpClient httpClient;
//        private readonly IAuthenticator authenticator;
//        private readonly Func<TDomain> createDomain;
//        private readonly Dictionary<TableStorageFetchKey, ContinuationToken> fetchHistory;

//        public GenericRepository(IConfigurationProvider configuration, IHttpClient httpClient,
//            IAuthenticator authenticator, Func<TDomain> createDomain)
//        {
//            this.fetchHistory = [];
//            this.CachedData = [];
//            this.configuration = configuration;
//            this.httpClient = httpClient;
//            this.authenticator = authenticator;
//            this.createDomain = createDomain;
//            this.TableAPIBaseUrL = this.configuration.GetSettingAsString(Constants.KEY_TABLE_URL);
//        }

//        protected List<TDomain> CachedData { get; private set; }

//        public virtual IReadOnlyCollection<TDomain> Data => this.CachedData;

//        protected virtual string TableName => typeof(TEntity).Name;

//        public virtual string TableAPIBaseUrL { get; set; }

//        public async Task<OperationResult<TDomain>> LoadAsync(string partitionKey, string rowKey, bool force = false)
//        {
//            var result = await this.LoadAsync(partitionKey, partitionKey, rowKey, rowKey, 1, force);
//            if (!result.IsSuccess)
//            {
//                return new OperationResult<TDomain>(result);
//            }

//            var single = result.Result.SingleOrDefault(c => c.PartitionKey == partitionKey && c.RowKey == rowKey);
//            if (single == null && force)
//            {
//                // REMARK (5he11) 如果加载某一精确数据，而且要求强制加载，如果该数据不存在，则应该从缓存中删除，因为这个可能是删除之后的“刷新”操作
//                this.Uncache(partitionKey, rowKey);
//            }

//            return new OperationResult<TDomain>(single);
//        }

//        public async Task<OperationResult<IReadOnlyCollection<TDomain>>> LoadAsync(string partitionKey, int count = -1, bool force = false, bool continueToLoadMore = false)
//            => await this.LoadAsync(partitionKey, partitionKey, null, null, count, force, continueToLoadMore);

//        public async Task<OperationResult<IReadOnlyCollection<TDomain>>> LoadAsync(string partitionKey, string rowKeyStart, string rowKeyEnd, int count = -1, bool force = false, bool continueToLoadMore = false)
//            => await this.LoadAsync(partitionKey, partitionKey, rowKeyStart, rowKeyEnd, count, force, continueToLoadMore);

//        public async Task<OperationResult<IReadOnlyCollection<TDomain>>> LoadAsync(string partitionKeyStart, string partitionKeyEnd, int count = -1, bool force = false, bool continueToLoadMore = false)
//            => await this.LoadAsync(partitionKeyStart, partitionKeyEnd, null, null, count, force, continueToLoadMore);

//        public async Task<OperationResult<IReadOnlyCollection<TDomain>>> LoadAsync(string partitionKeyStart, string partitionKeyEnd, string rowKeyStart, string rowKeyEnd, int count = -1, bool force = false, bool continueToLoadMore = false)
//        {
//            IReadOnlyCollection<TDomain> result = new List<TDomain>();
//            var key = new TableStorageFetchKey
//            {
//                Count = count,
//                PartitionKeyEnd = partitionKeyEnd,
//                PartitionKeyStart = partitionKeyStart,
//                RowKeyEnd = rowKeyEnd,
//                RowKeyStart = rowKeyStart,
//            };

//            if (!force
//                && partitionKeyStart != null && rowKeyStart != null
//                && partitionKeyStart == partitionKeyEnd && rowKeyStart == rowKeyEnd)
//            {
//                // REMARK (5he11) 这种情况下是查询一个准确命中的数据，并且不要求强制刷新数据，所以应该看一下数据是否有缓存，有则跳过网络请求
//                var cache = this.CachedData.SingleOrDefault(c => c.PartitionKey == partitionKeyStart && c.RowKey == rowKeyStart);

//                if (cache != null)
//                {
//                    return new OperationResult<IReadOnlyCollection<TDomain>>(new[] { cache });
//                }
//            }

//            var proceed = false;
//            ContinuationToken continuationToken = null;
//            if (force || !this.fetchHistory.ContainsKey(key))
//            {
//                if (force && continueToLoadMore)
//                {
//                    throw new NotSupportedException($"{nameof(force)} and {nameof(continueToLoadMore)} cannot be both true.");
//                }

//                proceed = true;
//            }
//            else if (fetchHistory.TryGetValue(key, out ContinuationToken value))
//            {
//                if (continueToLoadMore)
//                {
//                    continuationToken = value;
//                    proceed = true;
//                }
//            }
//            else
//            {
//                throw new NotImplementedException("TODO");
//            }

//            if (proceed)
//            {
//                var response = await this.FetchAsync(partitionKeyStart, partitionKeyEnd, rowKeyStart, rowKeyEnd, count, continuationToken);
//                if (!response.IsSuccess)
//                {
//                    return new OperationResult<IReadOnlyCollection<TDomain>>(response);
//                }
//                else
//                {
//                    this.fetchHistory.Remove(key);
//                    this.fetchHistory.Add(key, response.Result.ContinuationToken);

//                    if (response.Result.Data.Count > 0)
//                    {
//                        result = await this.Cache(response.Result.Data);
//                    }
//                }
//            }

//            return new OperationResult<IReadOnlyCollection<TDomain>>(result);
//        }

//        public async Task<OperationResult<IReadOnlyCollection<TDomain>>> LoadAsync(int count = -1, bool force = false, bool continueToLoadMore = false)
//            => await this.LoadAsync(null, null, null, null, count, force, continueToLoadMore);

//        protected virtual async Task<OperationResult<TableQueryResult<TEntity>>> FetchAsync(string partitionKeyStart, string partitionKeyEnd, string rowKeyStart, string rowKeyEnd, int count, ContinuationToken continuationToken)
//        {
//            string pk, rk;
//            if (partitionKeyStart == null && partitionKeyEnd == null)
//            {
//                pk = null;
//            }
//            else if (partitionKeyStart == partitionKeyEnd)
//            {
//                pk = partitionKeyStart;
//            }
//            else if (partitionKeyStart != null && partitionKeyEnd != null && partitionKeyStart != partitionKeyEnd)
//            {
//                if (partitionKeyStart == partitionKeyEnd[..^1] && partitionKeyEnd.EndsWith('~'))
//                {
//                    pk = partitionKeyEnd;
//                }
//                else
//                {
//                    throw new NotImplementedException("TODO: querying on special partition key range has not yet implemented.");
//                }
//            }
//            else
//            {
//                throw new NotSupportedException();
//            }

//            if (rowKeyStart == null && rowKeyEnd == null)
//            {
//                rk = null;
//            }
//            else if (rowKeyStart == rowKeyEnd)
//            {
//                rk = rowKeyStart;
//            }
//            else if (rowKeyStart != null && rowKeyEnd != null && rowKeyStart != rowKeyEnd)
//            {
//                // REMARK (5he11) 这种情况仅在签名上请求PK，而在查询过滤条件上过滤RK
//                rk = null;
//            }
//            else
//            {
//                throw new NotSupportedException();
//            }


//            var signature = await this.authenticator.AquireSignatureAsync(ResourceType.AzureStorageTable, this.TableName, pk, rk);
//            if (!signature.IsSuccess)
//            {
//                return new OperationResult<TableQueryResult<TEntity>>(signature);
//            }

//            return await TableStorageHelper.GetAsync<TEntity>(this.httpClient, this.TableAPIBaseUrL, this.TableName, signature.Result.Signature, partitionKeyStart, partitionKeyEnd, rowKeyStart, rowKeyEnd, continuationToken, count);
//        }

//        protected virtual async Task<IReadOnlyCollection<TDomain>> Cache(IEnumerable<TEntity> entities)
//        {
//            var result = new List<TDomain>();

//            foreach (var entity in entities)
//            {
//                var domain = this.ToDomain(entity);
//                var existing = this.CachedData.SingleOrDefault(d => d.PartitionKey == domain.PartitionKey && d.RowKey == domain.RowKey);
//                if (existing != null)
//                {
//                    var existingETag = await existing.GetHashAsync();
//                    var currentETag = await domain.GetHashAsync();
//                    if (existingETag != currentETag)
//                    {
//                        var i = this.CachedData.IndexOf(existing);
//                        this.CachedData[i] = domain;
//                        result.Add(domain);
//                    }
//                    else
//                    {
//                        result.Add(existing);
//                    }
//                }
//                else
//                {
//                    this.CachedData.Add(domain);
//                    result.Add(domain);
//                }
//            }

//            return result;
//        }

//        public void Uncache(string partitionKey, string rowKey)
//            => this.CachedData.RemoveAll(c => partitionKey == c.PartitionKey && rowKey == c.RowKey);

//        public void Uncache(TDomain domainObject) => this.Uncache(new[] { domainObject });

//        public void Uncache(IEnumerable<TDomain> domainObjects)
//            => this.CachedData.RemoveAll(c => domainObjects.Any(dobj => dobj.PartitionKey == c.PartitionKey && dobj.RowKey == c.RowKey));

//        protected virtual TDomain ToDomain(TEntity entity) => (TDomain)this.createDomain().Initialize(entity);

//        protected struct TableStorageFetchKey : IEquatable<TableStorageFetchKey>
//        {
//            public string PartitionKeyStart { get; set; }

//            public string PartitionKeyEnd { get; set; }

//            public string RowKeyStart { get; set; }

//            public string RowKeyEnd { get; set; }

//            public int Count { get; set; }

//            public override readonly bool Equals(object obj) => obj is TableStorageFetchKey key && this.Equals(key);

//            public readonly bool Equals(TableStorageFetchKey other) => this.PartitionKeyStart == other.PartitionKeyStart && this.PartitionKeyEnd == other.PartitionKeyEnd && this.RowKeyStart == other.RowKeyStart && this.RowKeyEnd == other.RowKeyEnd && this.Count == other.Count;

//            public override readonly int GetHashCode()
//            {
//                return HashCode.Combine(PartitionKeyStart, PartitionKeyEnd, RowKeyStart, RowKeyEnd, Count);
//            }

//            public static bool operator ==(TableStorageFetchKey left, TableStorageFetchKey right) => left.Equals(right);

//            public static bool operator !=(TableStorageFetchKey left, TableStorageFetchKey right) => !(left == right);
//        }
//    }
//}
