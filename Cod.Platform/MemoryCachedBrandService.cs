namespace Cod.Platform
{
    internal class MemoryCachedBrandService : IBrandService
    {
        private static IEnumerable<BrandingInfo> cache;
        private readonly Lazy<IRepository<BrandingInfo>> repository;

        public MemoryCachedBrandService(Lazy<IRepository<BrandingInfo>> repository) => this.repository = repository;

        public async Task<BrandingInfo> GetAsync(string name)
        {
            name = name.Trim().ToUpperInvariant();
            cache ??= await this.repository.Value.GetAsync().ToListAsync();

            var result = cache.SingleOrDefault(b => b.PartitionKey == name);
            if (result == null)
            {
                cache = await this.repository.Value.GetAsync().ToListAsync();
            }

            return cache.SingleOrDefault(b => b.PartitionKey == name);
        }

        public async Task<BrandingInfo> GetAsync(OpenIDKind kind, string app)
        {
            app = app.Trim();

            if (kind != OpenIDKind.Wechat)
            {
                throw new NotSupportedException();
            }

            cache ??= await this.repository.Value.GetAsync().ToListAsync();
            var result = cache.SingleOrDefault(b => b.WechatAppID == app);
            if (result == null)
            {
                cache = await this.repository.Value.GetAsync().ToListAsync();
            }

            return cache.SingleOrDefault(b => b.WechatAppID == app);
        }
    }
}
