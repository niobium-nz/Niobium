using System;
using System.Threading.Tasks;

namespace Cod.Platform
{
    public class BusinessDomain : PlatformDomain<Model.Business>, IAccountable
    {
        private readonly Lazy<ICacheStore> cache;

        public BusinessDomain(Lazy<IRepository<Model.Business>> repository, Lazy<ICacheStore> cache) : base(repository)
        {
            this.cache = cache;
        }

        public ICacheStore CacheStore => this.cache.Value;

        public Task<string> GetAccountingPrincipalAsync() => Task.FromResult(this.RowKey);
    }
}
