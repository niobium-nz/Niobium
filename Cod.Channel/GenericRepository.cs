using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Cod.Contract;

namespace Cod.Channel
{
    public class GenericRepository<T> : IRepository<T>
    {
        private readonly List<IDomain<T>> cache;
        private readonly IConfigurationProvider configuration;
        private readonly HttpClient httpClient;
        private readonly IAuthenticator authenticator;
        private readonly Func<IDomain<T>> createDomain;

        public GenericRepository(IConfigurationProvider configuration, HttpClient httpClient,
            IAuthenticator authenticator, Func<IDomain<T>> createDomain)
        {
            this.cache = new List<IDomain<T>>();
            this.configuration = configuration;
            this.httpClient = httpClient;
            this.authenticator = authenticator;
            this.createDomain = createDomain;
        }

        public IReadOnlyCollection<IDomain<T>> Data => this.cache;

        public async Task<ContinuationToken> LoadAsync(string partitionKey, string rowKey)
            => await this.LoadAsync(partitionKey, partitionKey, rowKey, rowKey, 1);

        public async Task<ContinuationToken> LoadAsync(string partitionKey, int count = -1)
            => await this.LoadAsync(partitionKey, partitionKey, null, null, count);

        public async Task<ContinuationToken> LoadAsync(string partitionKey, string rowKeyStart, string rowKeyEnd, int count = -1)
            => await this.LoadAsync(partitionKey, partitionKey, rowKeyStart, rowKeyEnd, count);

        public async Task<ContinuationToken> LoadAsync(string partitionKeyStart, string partitionKeyEnd, int count = -1)
            => await this.LoadAsync(partitionKeyStart, partitionKeyEnd, null, null, count);

        public async Task<ContinuationToken> LoadAsync(string partitionKeyStart, string partitionKeyEnd, string rowKeyStart, string rowKeyEnd, int count = -1)
        {
            var result = await this.FetchAsync(partitionKeyStart, partitionKeyEnd, rowKeyStart, rowKeyEnd, count);
            var domainObjects = result.Data.Select(m => this.createDomain().Initialize(m));
            this.cache.AddRange(domainObjects);
            return result.ContinuationToken;
        }

        public async Task<ContinuationToken> LoadAsync(int count = -1)
            => await this.LoadAsync(null, null, null, null, count);

        private async Task<TableQueryResult<T>> FetchAsync(string partitionKeyStart, string partitionKeyEnd, string rowKeyStart, string rowKeyEnd, int count)
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


            var signature = await this.authenticator.AquireSignatureAsync(StorageType.Table, typeof(T).Name, pk, rk);
            if (!signature.IsSuccess)
            {
                return new TableQueryResult<T>
                {
                    Data = new List<T>(),
                };
            }

            var baseUrl = await this.configuration.GetSettingAsync(Constants.KEY_TABLE_URL);
            return await TableStorageHelper.GetAsync<T>(httpClient, baseUrl, signature.Result.Signature, partitionKeyStart, partitionKeyEnd, rowKeyStart, rowKeyEnd, null, count);
        }

    }
}
