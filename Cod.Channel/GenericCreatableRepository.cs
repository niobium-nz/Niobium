using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Cod.Channel
{
    public abstract class GenericCreatableRepository<TDomain, TEntity, TCreateParams>
        : GenericRepository<TDomain, TEntity>, ICreatableRepository<TDomain, TEntity, TCreateParams>
        where TDomain : IChannelDomain<TEntity>
        where TCreateParams : class
    {
        public GenericCreatableRepository(
            IConfigurationProvider configuration,
            HttpClient httpClient,
            IAuthenticator authenticator,
            Func<TDomain> createDomain)
            : base(configuration, httpClient, authenticator, createDomain)
        {
        }

        public virtual async Task<OperationResult<TDomain>> CreateAsync(TCreateParams parameters)
        {
            TDomain newEntity = default;
            var result = await this.CreateCoreAsync(parameters);
            if (result.IsSuccess)
            {
                var keys = result.Result;
                newEntity = await this.LoadAsync(keys.PartitionKey, keys.RowKey, true);
                if (newEntity != null)
                {
                    this.AddToCache(newEntity);
                }
                else
                {
                    //REMARK (5he11) 这个情况不太可能出现，如果真的出现了，那就是存储层有较大延迟，刚存入的数据无法读出导致的
                    throw new NotImplementedException("Please refer to comment in source code.");
                }
            }
            return new OperationResult<TDomain>(result.Code, newEntity)
            {
                Reference = result.Reference,
                Message = result.Message,
            };
        }

        protected abstract Task<OperationResult<StorageKey>> CreateCoreAsync(TCreateParams parameters);
    }
}
