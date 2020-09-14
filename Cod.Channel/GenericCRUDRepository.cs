using System;
using System.Linq;
using System.Threading.Tasks;

namespace Cod.Channel
{
    public abstract class GenericCRUDRepository<TDomain, TEntity>
        : GenericCRUDRepository<TDomain, TEntity, TEntity>,
        ICRUDRepository<TDomain, TEntity>
        where TEntity : class, IEntity
        where TDomain : IChannelDomain<TEntity>
    {
        public GenericCRUDRepository(IConfigurationProvider configuration, IHttpClient httpClient, IAuthenticator authenticator, Func<TDomain> createDomain)
            : base(configuration, httpClient, authenticator, createDomain)
        {
        }
    }

    public abstract class GenericCRUDRepository<TDomain, TEntity, TCreateParams>
        : GenericCRUDRepository<TDomain, TEntity, TCreateParams, TCreateParams>,
        ICRUDRepository<TDomain, TEntity, TCreateParams>
        where TEntity : IEntity
        where TDomain : IChannelDomain<TEntity>
        where TCreateParams : class
    {
        public GenericCRUDRepository(IConfigurationProvider configuration, IHttpClient httpClient, IAuthenticator authenticator, Func<TDomain> createDomain)
            : base(configuration, httpClient, authenticator, createDomain)
        {
        }
    }

    public abstract class GenericCRUDRepository<TDomain, TEntity, TCreateParams, TUpdateParams>
        : GenericRepository<TDomain, TEntity>,
        ICreatableRepository<TDomain, TEntity, TCreateParams>,
        IUpdatableRepository<TDomain, TEntity, TUpdateParams>,
        IDeletableRepository<TDomain, TEntity>,
        ICRUDRepository<TDomain, TEntity, TCreateParams, TUpdateParams>
        where TEntity : IEntity
        where TDomain : IChannelDomain<TEntity>
        where TCreateParams : class
    {
        public GenericCRUDRepository(
            IConfigurationProvider configuration,
            IHttpClient httpClient,
            IAuthenticator authenticator,
            Func<TDomain> createDomain)
            : base(configuration, httpClient, authenticator, createDomain)
        {
        }

        public virtual async Task<OperationResult<TDomain>> CreateAsync(TCreateParams parameters)
        {
            var result = await this.CreateCoreAsync(parameters);
            if (!result.IsSuccess)
            {
                return new OperationResult<TDomain>(result);
            }

            var keys = result.Result;
            return await this.LoadAsync(keys.PartitionKey, keys.RowKey, true);
        }

        public async Task<OperationResult> DeleteAsync(StorageKey key)
        {
            var domain = this.Data.SingleOrDefault(d => d.PartitionKey == key.PartitionKey && d.RowKey == key.RowKey);
            if (domain != null)
            {
                var result = await this.DeleteCoreAsync(domain.Entity);
                if (result.IsSuccess)
                {
                    this.Uncache(domain);
                }
                return result;
            }
            return OperationResult.Success;
        }

        public async Task<OperationResult<TDomain>> UpdateAsync(TUpdateParams parameters)
        {
            var result = await this.UpdateCoreAsync(parameters);
            if (!result.IsSuccess)
            {
                return new OperationResult<TDomain>(result);
            }

            var keys = result.Result;
            return await this.LoadAsync(keys.PartitionKey, keys.RowKey, true);
        }

        protected virtual Task<OperationResult<StorageKey>> UpdateCoreAsync(TUpdateParams parameters) => throw new NotImplementedException();

        protected virtual Task<OperationResult<StorageKey>> CreateCoreAsync(TCreateParams parameters) => throw new NotImplementedException();

        protected virtual Task<OperationResult> DeleteCoreAsync(TEntity entity) => throw new NotImplementedException();
    }
}
