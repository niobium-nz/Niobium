using System;
using System.Linq;
using System.Threading.Tasks;
using Cod.Platform.Model;

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
            throw new ArgumentOutOfRangeException($"The specified branding info cannot be found: {brand}");
        }

        public static async Task<BrandingInfo> GetAsync(this IRepository<BrandingInfo> repository, OpenIDProvider provider, string appID)
        {
            if (provider == OpenIDProvider.Wechat)
            {
                var results = await repository.GetAsync(100);
                var result = results.SingleOrDefault(r => r.WechatAppID == appID);
                if (result != null)
                {
                    return result;
                }

                throw new ArgumentOutOfRangeException($"The specified branding info cannot be found: {appID}");
            }
            throw new NotSupportedException();
        }
    }
}
