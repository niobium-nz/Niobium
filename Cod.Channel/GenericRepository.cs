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

        protected List<TDomain> Cache { get; private set; }

        public GenericRepository(IConfigurationProvider configuration, HttpClient httpClient,
            IAuthenticator authenticator, Func<TDomain> createDomain)
        {
            this.Cache = new List<TDomain>();
            this.configuration = configuration;
            this.httpClient = httpClient;
            this.authenticator = authenticator;
            this.createDomain = createDomain;
        }

        public IReadOnlyCollection<TDomain> Data => this.Cache;

        public async Task<TDomain> LoadAsync(string partitionKey, string rowKey)
        {
            var result = await this.FetchAsync(partitionKey, partitionKey, rowKey, rowKey, 1);
            var domainObjects = result.Data.Select(m => (TDomain)this.createDomain().Initialize(m));
            return domainObjects.SingleOrDefault();
        }

        public async Task<ContinuationToken> LoadAsync(string partitionKey, int count = -1)
            => await this.LoadAsync(partitionKey, partitionKey, null, null, count);

        public async Task<ContinuationToken> LoadAsync(string partitionKey, string rowKeyStart, string rowKeyEnd, int count = -1)
            => await this.LoadAsync(partitionKey, partitionKey, rowKeyStart, rowKeyEnd, count);

        public async Task<ContinuationToken> LoadAsync(string partitionKeyStart, string partitionKeyEnd, int count = -1)
            => await this.LoadAsync(partitionKeyStart, partitionKeyEnd, null, null, count);

        public async Task<ContinuationToken> LoadAsync(string partitionKeyStart, string partitionKeyEnd, string rowKeyStart, string rowKeyEnd, int count = -1)
        {
            var result = await this.FetchAsync(partitionKeyStart, partitionKeyEnd, rowKeyStart, rowKeyEnd, count);
            var domainObjects = result.Data.Select(m => (TDomain)this.createDomain().Initialize(m));
            this.Cache.AddRange(domainObjects);
            return result.ContinuationToken;
        }

        public async Task<ContinuationToken> LoadAsync(int count = -1)
            => await this.LoadAsync(null, null, null, null, count);

        protected virtual async Task<TableQueryResult<TEntity>> FetchAsync(string partitionKeyStart, string partitionKeyEnd, string rowKeyStart, string rowKeyEnd, int count)
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
                return new TableQueryResult<TEntity>
                {
                    Data = new List<TEntity>(),
                };
            }

            var baseUrl = await this.configuration.GetSettingAsync(Constants.KEY_TABLE_URL);
            return await TableStorageHelper.GetAsync<TEntity>(this.httpClient, baseUrl, signature.Result.Signature, partitionKeyStart, partitionKeyEnd, rowKeyStart, rowKeyEnd, null, count);
        }

    }
}
