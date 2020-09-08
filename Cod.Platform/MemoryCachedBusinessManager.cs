using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Cod.Platform
{
    internal class MemoryCachedBusinessManager : IBusinessManager
    {
        private static IEnumerable<Business> cache;
        private readonly Lazy<IRepository<Business>> repository;

        public MemoryCachedBusinessManager(Lazy<IRepository<Business>> repository) => this.repository = repository;

        public async Task<Business> GetAsync(Guid id)
        {
            if (cache == null)
            {
                cache = await this.repository.Value.GetAsync();
            }

            var result = cache.SingleOrDefault(b => b.GetID() == id);
            if (result == null)
            {
                cache = await this.repository.Value.GetAsync();
            }

            return cache.SingleOrDefault(b => b.GetID() == id);
        }
    }
}
