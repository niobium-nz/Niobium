using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Cod.Platform
{
    public class UserDomain : ImpedableDomain<Model.User>, IAccountable
    {
        private readonly Lazy<ICacheStore> cache;

        public UserDomain(
            Lazy<ICacheStore> cache,
            Lazy<IRepository<Model.User>> repository,
            Lazy<IEnumerable<IImpedimentPolicy>> policies,
            ILogger logger)
            : base(repository, policies, logger)
        {
            this.cache = cache;
        }

        public ICacheStore CacheStore => this.cache.Value;

        public Task<string> GetAccountingPrincipalAsync() => Task.FromResult(this.RowKey);
    }
}
