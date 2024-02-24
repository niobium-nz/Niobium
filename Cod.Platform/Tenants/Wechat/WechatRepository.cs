using Cod.Platform.Database;
using Microsoft.Extensions.Logging;

namespace Cod.Platform.Tenants.Wechat
{
    public class WechatRepository : IRepository<WechatEntity>
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

        public async Task<WechatEntity> RetrieveAsync(string partitionKey, string rowKey, IList<string> fields = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(partitionKey))
            {
                throw new ArgumentNullException(nameof(partitionKey));
            }

            if (string.IsNullOrWhiteSpace(rowKey))
            {
                throw new ArgumentNullException(nameof(rowKey));
            }

            string appID = partitionKey;
            string feature = rowKey;

            BrandingInfo branding = await brandService.Value.GetAsync(OpenIDKind.Wechat, appID);
            if (branding == null)
            {
                return null;
            }
            if (feature == WechatEntity.BuildAPITicketRowKey())
            {
                OperationResult<string> result = await wechatIntegration.Value.GetJSApiTicket(appID, branding.WechatSecret);
                if (!result.IsSuccess)
                {
                    logger.LogError($"获取微信JSAPI令牌失败: {result.Message} 参考: {result.Reference}");
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
                OperationResult<string> result = await wechatIntegration.Value.GetOpenIDAsync(appID, branding.WechatSecret, feature);
                if (!result.IsSuccess)
                {
                    logger.LogError($"获取微信OpenID失败: {result.Message} 参考: {result.Reference}");
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

        public Task<IEnumerable<WechatEntity>> CreateAsync(IEnumerable<WechatEntity> entities, bool replaceIfExist = false, DateTimeOffset? expiry = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task DeleteAsync(IEnumerable<WechatEntity> entities, bool preconditionCheck = true, bool successIfNotExist = false, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<TableQueryResult<WechatEntity>> GetAsync(int limit, string continuationToken = null, IList<string> fields = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<TableQueryResult<WechatEntity>> GetAsync(string partitionKey, int limit, string continuationToken = null, IList<string> fields = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<WechatEntity>> UpdateAsync(IEnumerable<WechatEntity> entities, bool preconditionCheck = true, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public IAsyncEnumerable<WechatEntity> GetAsync(IList<string> fields = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public IAsyncEnumerable<WechatEntity> GetAsync(string partitionKey, IList<string> fields = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
