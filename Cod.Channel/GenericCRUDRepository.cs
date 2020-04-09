using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Cod.Channel
{
    public abstract class GenericCRUDRepository<TDomain, TEntity, TCreateParams>
        : GenericRepository<TDomain, TEntity>,
        ICreatableRepository<TDomain, TEntity, TCreateParams>,
        IUpdatableRepository<TDomain, TEntity>,
        IDeletableRepository<TDomain, TEntity>,
        ICRUDRepository<TDomain, TEntity, TCreateParams>
        where TEntity : IEntity
        where TDomain : IChannelDomain<TEntity>
        where TCreateParams : class
    {
        public GenericCRUDRepository(
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
                var loadResult = await this.LoadAsync(keys.PartitionKey, keys.RowKey, true);
                if (!loadResult.IsSuccess)
                {
                    return loadResult;
                }

                newEntity = loadResult.Result;
                if (newEntity != null)
                {
                    this.Cache(newEntity);
                }
                else
                {
                    //REMARK (5he11) This seems not possible but if it ever occurred,
                    // it means the storage layer has high latency causes data retrieved does not reflect its realtime state.
                    throw new NotImplementedException("Please refer to comment in source code.");
                }
            }
            return new OperationResult<TDomain>(result.Code, newEntity)
            {
                Reference = result.Reference,
                Message = result.Message,
            };
        }

        public async Task<OperationResult> DeleteAsync(TEntity entity)
        {
            var result = await this.DeleteCoreAsync(entity);
            if (result.IsSuccess)
            {
                this.Uncache(entity);
            }
            return result;
        }

        public async Task<OperationResult<TDomain>> UpdateAsync(TEntity entity)
        {
            TDomain newEntity = default;
            var result = await this.UpdateCoreAsync(entity);
            if (result.IsSuccess)
            {
                var loadResult = await this.LoadAsync(entity.PartitionKey, entity.RowKey, true);
                if (!loadResult.IsSuccess)
                {
                    return loadResult;
                }

                newEntity = loadResult.Result;
                if (newEntity != null)
                {
                    this.Cache(newEntity);
                }
                else
                {
                    //REMARK (5he11) This seems not possible but if it ever occurred,
                    // it means the storage layer has high latency causes data retrieved does not reflect its realtime state.
                    throw new NotImplementedException("Please refer to comment in source code.");
                }
            }
            return new OperationResult<TDomain>(result.Code, newEntity)
            {
                Reference = result.Reference,
                Message = result.Message,
            };
        }

        protected virtual Task<OperationResult> UpdateCoreAsync(TEntity entity) => throw new NotImplementedException();

        protected virtual Task<OperationResult<StorageKey>> CreateCoreAsync(TCreateParams parameters) => throw new NotImplementedException();

        protected virtual Task<OperationResult> DeleteCoreAsync(TEntity entity) => throw new NotImplementedException();
    }
}
