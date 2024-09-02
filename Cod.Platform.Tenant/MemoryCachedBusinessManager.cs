namespace Cod.Platform.Tenant
{
    internal class MemoryCachedBusinessManager : IBusinessManager
    {
        private static IEnumerable<Business> cache;
        private readonly Lazy<IRepository<Business>> repository;

        public MemoryCachedBusinessManager(Lazy<IRepository<Business>> repository)
        {
            this.repository = repository;
        }

        public async Task<Business> GetAsync(Guid id)
        {
            cache ??= await repository.Value.GetAsync().ToListAsync();

            Business result = cache.SingleOrDefault(b => b.GetID() == id);
            if (result == null)
            {
                cache = await repository.Value.GetAsync().ToListAsync();
            }

            return cache.SingleOrDefault(b => b.GetID() == id);
        }
    }
}
