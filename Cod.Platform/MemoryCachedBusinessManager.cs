using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Cod.Platform
{
    internal class MemoryCachedBusinessManager : IBusinessManager
    {
        private static IEnumerable<Model.Business> cache;
        private readonly Lazy<IRepository<Model.Business>> repository;

        public MemoryCachedBusinessManager(Lazy<IRepository<Model.Business>> repository)
        {
            this.repository = repository;
        }

        public async Task<Model.Business> GetAsync(Guid id)
        {
            if (cache == null)
            {
                cache = await repository.Value.GetAsync();
            }

            var result = cache.SingleOrDefault(b => b.GetID() == id);
            if (result == null)
            {
                cache = await repository.Value.GetAsync();
            }

            return cache.SingleOrDefault(b => b.GetID() == id);
        }
    }
}
