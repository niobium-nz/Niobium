//using System;
//using System.Linq;
//using System.Threading.Tasks;

//namespace Cod.Channel
//{
//    public abstract class GenericCRUDRepository<TDomain, TEntity>(IConfigurationProvider configuration, IHttpClient httpClient, IAuthenticator authenticator, Func<TDomain> createDomain)
//        : GenericCRUDRepository<TDomain, TEntity, TEntity>(configuration, httpClient, authenticator, createDomain),
//        ICRUDRepository<TDomain, TEntity>
//        where TEntity : class, new()
//        where TDomain : IDomain<TEntity>
//    {
//    }

//    public abstract class GenericCRUDRepository<TDomain, TEntity, TCreateParams>(IConfigurationProvider configuration, IHttpClient httpClient, IAuthenticator authenticator, Func<TDomain> createDomain)
//        : GenericCRUDRepository<TDomain, TEntity, TCreateParams, TCreateParams>(configuration, httpClient, authenticator, createDomain),
//        ICRUDRepository<TDomain, TEntity, TCreateParams>
//        where TEntity : class, new()
//        where TDomain : IDomain<TEntity>
//        where TCreateParams : class
//    {
//    }

//    public abstract class GenericCRUDRepository<TDomain, TEntity, TCreateParams, TUpdateParams>(
//        IConfigurationProvider configuration,
//        IHttpClient httpClient,
//        IAuthenticator authenticator,
//        Func<TDomain> createDomain)
//        : GenericRepository<TDomain, TEntity>(configuration, httpClient, authenticator, createDomain),
//        ICreatableRepository<TDomain, TEntity, TCreateParams>,
//        IUpdatableRepository<TDomain, TEntity, TUpdateParams>,
//        IDeletableRepository<TDomain, TEntity>,
//        ICRUDRepository<TDomain, TEntity, TCreateParams, TUpdateParams>
//        where TEntity : class, new()
//        where TDomain : IDomain<TEntity>
//        where TCreateParams : class
//    {
//        public virtual async Task<OperationResult<TDomain>> CreateAsync(TCreateParams parameters)
//        {
//            var result = await this.CreateCoreAsync(parameters);
//            if (!result.IsSuccess)
//            {
//                return new OperationResult<TDomain>(result);
//            }

//            var keys = result.Result;
//            return await this.LoadAsync(keys.PartitionKey, keys.RowKey, true);
//        }

//        public async Task<OperationResult> DeleteAsync(StorageKey key)
//        {
//            var domain = this.Data.SingleOrDefault(d => d.PartitionKey == key.PartitionKey && d.RowKey == key.RowKey);
//            if (domain != null)
//            {
//                var entity = await domain.GetEntityAsync();
//                var result = await this.DeleteCoreAsync(entity);
//                if (result.IsSuccess)
//                {
//                    this.Uncache(domain);
//                }
//                return result;
//            }
//            return OperationResult.Success;
//        }

//        public async Task<OperationResult<TDomain>> UpdateAsync(TUpdateParams parameter)
//        {
//            var result = await this.UpdateCoreAsync(parameter);
//            if (!result.IsSuccess)
//            {
//                return new OperationResult<TDomain>(result);
//            }

//            var keys = result.Result;
//            return await this.LoadAsync(keys.PartitionKey, keys.RowKey, true);
//        }

//        protected virtual Task<OperationResult<StorageKey>> UpdateCoreAsync(TUpdateParams parameters) => throw new NotImplementedException();

//        protected virtual Task<OperationResult<StorageKey>> CreateCoreAsync(TCreateParams parameters) => throw new NotImplementedException();

//        protected virtual Task<OperationResult> DeleteCoreAsync(TEntity entity) => throw new NotImplementedException();
//    }
//}
