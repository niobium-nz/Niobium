using System.Linq;
using System.Threading.Tasks;

namespace Cod.Platform
{
    public static class IBrandingRepositoryExtensions
    {
        public static async Task<BrandingInfo> GetByBrandAsync(this IRepository<BrandingInfo> repository, string brand)
        {
            var result = await repository.GetAsync(BrandingInfo.BuildPartitionKey(brand), 100);
            if (result.Count == 1)
            {
                return result[0];
            }
            return null;
        }

        public static async Task<BrandingInfo> GetAsync(this IRepository<BrandingInfo> repository, OpenIDProvider provider, string appID)
        {
            if (provider == OpenIDProvider.Wechat)
            {
                var results = await repository.GetAsync(100);
                return results.SingleOrDefault(r => r.WechatAppID == appID);
            }
            return null;
        }
    }
}
