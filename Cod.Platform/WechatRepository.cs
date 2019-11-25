using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cod.Contract;

namespace Cod.Platform
{
    internal class WechatRepository : IRepository<WechatEntity>
    {
        private readonly Lazy<IRepository<BrandingInfo>> repository;

        public WechatRepository(Lazy<IRepository<BrandingInfo>> repository) => this.repository = repository;

        public async Task<WechatEntity> GetAsync(string appID, string feature)
        {
            if (String.IsNullOrWhiteSpace(appID))
            {
                throw new ArgumentNullException(nameof(appID));
            }

            if (String.IsNullOrWhiteSpace(feature))
            {
                throw new ArgumentNullException(nameof(feature));
            }

            var branding = await this.repository.Value.GetAsync(OpenIDProvider.Wechat, appID);
            if (branding == null)
            {
                return null;
            }
            if (feature == WechatEntity.BuildAPITicketRowKey())
            {
                var result = await WechatHelper.GetJSApiTicket(appID, branding.WechatSecret);
                return new WechatEntity
                {
                    PartitionKey = appID,
                    RowKey = feature,
                    Value = result
                };
            }
            else
            {
                var result = await WechatHelper.GetOpenIDAsync(appID, branding.WechatSecret, feature);
                return new WechatEntity
                {
                    PartitionKey = appID,
                    RowKey = feature,
                    Value = result
                };
            }
        }

        public Task<IEnumerable<WechatEntity>> CreateAsync(IEnumerable<WechatEntity> entities, bool replaceIfExist) => throw new NotImplementedException();

        public Task<TableQueryResult<WechatEntity>> GetAsync(int limit) => throw new NotImplementedException();

        public Task<TableQueryResult<WechatEntity>> GetAsync(string partitionKey, int limit) => throw new NotImplementedException();

        public Task<IEnumerable<WechatEntity>> UpdateAsync(IEnumerable<WechatEntity> entities) => throw new NotImplementedException();

        public Task<IEnumerable<WechatEntity>> DeleteAsync(IEnumerable<WechatEntity> entities) => throw new NotImplementedException();
    }
}
