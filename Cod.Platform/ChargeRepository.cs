using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cod.Platform.Model;
using Microsoft.Extensions.Logging;

namespace Cod.Platform
{
    public class ChargeRepository : IRepository<Charge>
    {
        private readonly Lazy<IRepository<BrandingInfo>> brandingRepository;
        private readonly Lazy<IConfigurationProvider> configuration;
        private readonly ILogger logger;

        public ChargeRepository(Lazy<IRepository<BrandingInfo>> brandingRepository,
            Lazy<IConfigurationProvider> configuration,
            ILogger logger)
        {
            this.brandingRepository = brandingRepository;
            this.configuration = configuration;
            this.logger = logger;
        }

        public async Task<IEnumerable<Charge>> CreateAsync(IEnumerable<Charge> entities, bool replaceIfExist)
        {
            if (entities.All(e => e.Provider == OpenIDProvider.Wechat && e.Type == ChargeType.WeChatJSAPI))
            {
                var charges = new List<Charge>();
                foreach (var charge in entities)
                {
                    var brandings = await this.brandingRepository.Value.GetAsync();
                    var branding = brandings.SingleOrDefault(b => b.WechatAppID == charge.AppID);
                    var key = await this.configuration.Value.GetSettingAsync("CHARGE_SECRET");
                    var toSign = $"{charge.AppID}|{charge.Account}|{charge.Amount}";
                    var internalSignature = SHA.SHA256Hash(toSign, key, 127);
                    var prepayid = await WechatHelper.JSAPIPay(charge.Account,
                        charge.Amount,
                        charge.AppID,
                        charge.Order,
                        charge.Product,
                        internalSignature,
                        charge.IP,
                        branding.WechatMerchantID,
                        branding.WechatMerchantNotifyUri,
                        branding.WechatMerchantSignature);
                    if (!prepayid.IsSuccess)
                    {
                        this.logger.LogError($"支付通道上游返回错误: {prepayid.Message} 参考: {prepayid.Reference}");
                        continue;
                    }

                    var paySignature = WechatHelper.GetJSAPIPaySignature(prepayid.Result, charge.AppID, branding.WechatMerchantSignature);
                    charge.Params = paySignature;
                    charges.Add(charge);
                }
                return charges;
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public Task<IEnumerable<Charge>> DeleteAsync(IEnumerable<Charge> entities) => throw new NotImplementedException();
        public Task<TableQueryResult<Charge>> GetAsync(int limit) => throw new NotImplementedException();
        public Task<TableQueryResult<Charge>> GetAsync(string partitionKey, int limit) => throw new NotImplementedException();
        public Task<Charge> GetAsync(string partitionKey, string rowKey) => throw new NotImplementedException();
        public Task<IEnumerable<Charge>> UpdateAsync(IEnumerable<Charge> entities) => throw new NotImplementedException();


    }
}
