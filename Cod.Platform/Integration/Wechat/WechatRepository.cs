using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cod.Platform.Model;
using Microsoft.Extensions.Logging;

namespace Cod.Platform
{
    internal class WechatRepository : IRepository<WechatEntity>
    {
        private readonly Lazy<IBrandService> brandService;
        private readonly Lazy<WechatIntegration> wechatIntegration;
        private readonly ILogger logger;

        public WechatRepository(Lazy<IBrandService> brandService,
            Lazy<WechatIntegration> wechatIntegration,
            ILogger logger)
        {
            this.brandService = brandService;
            this.wechatIntegration = wechatIntegration;
            this.logger = logger;
        }

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

            var branding = await this.brandService.Value.GetAsync(OpenIDKind.Wechat, appID);
            if (branding == null)
            {
                return null;
            }
            if (feature == WechatEntity.BuildAPITicketRowKey())
            {
                var result = await this.wechatIntegration.Value.GetJSApiTicket(appID, branding.WechatSecret);
                if (!result.IsSuccess)
                {
                    this.logger.LogError($"获取微信JSAPI令牌失败: {result.Message} 参考: {result.Reference}");
                    return new WechatEntity
                    {
                        PartitionKey = appID,
                        RowKey = feature,
                    };
                }
                return new WechatEntity
                {
                    PartitionKey = appID,
                    RowKey = feature,
                    Value = result.Result
                };
            }
            else
            {
                var result = await this.wechatIntegration.Value.GetOpenIDAsync(appID, branding.WechatSecret, feature);
                if (!result.IsSuccess)
                {
                    this.logger.LogError($"获取微信OpenID失败: {result.Message} 参考: {result.Reference}");
                    return new WechatEntity
                    {
                        PartitionKey = appID,
                        RowKey = feature,
                    };
                }
                return new WechatEntity
                {
                    PartitionKey = appID,
                    RowKey = feature,
                    Value = result.Result
                };
            }
        }

        public Task<IEnumerable<WechatEntity>> CreateAsync(IEnumerable<WechatEntity> entities, bool replaceIfExist) => throw new NotImplementedException();

        public Task<TableQueryResult<WechatEntity>> GetAsync(int limit) => throw new NotImplementedException();

        public Task<TableQueryResult<WechatEntity>> GetAsync(string partitionKey, int limit) => throw new NotImplementedException();

        public Task<IEnumerable<WechatEntity>> UpdateAsync(IEnumerable<WechatEntity> entities) => throw new NotImplementedException();

        public Task<IEnumerable<WechatEntity>> DeleteAsync(IEnumerable<WechatEntity> entities, bool successIfNotExist = false) => throw new NotImplementedException();

        public Task<IEnumerable<WechatEntity>> CreateOrUpdateAsync(IEnumerable<WechatEntity> entities) => throw new NotImplementedException();
    }
}
