using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Cod.Platform
{
    public class ChargeRepository : IRepository<Charge>
    {
        private readonly Lazy<IBrandService> brandService;
        private readonly Lazy<WechatIntegration> wechatIntegration;
        private readonly ILogger logger;

        public ChargeRepository(Lazy<IBrandService> brandService,
            Lazy<WechatIntegration> wechatIntegration,
            ILogger logger)
        {
            this.brandService = brandService;
            this.wechatIntegration = wechatIntegration;
            this.logger = logger;
        }

        public async Task<IEnumerable<Charge>> CreateAsync(IEnumerable<Charge> entities, bool replaceIfExist)
        {
            if (entities.All(e => e.OpenIDKind == OpenIDKind.Wechat && e.PaymentKind == PaymentKinds.Wechat))
            {
                var charges = new List<Charge>();
                foreach (var charge in entities)
                {
                    var branding = await this.brandService.Value.GetAsync(OpenIDKind.Wechat, charge.AppID);
                    var attach = $"{(int)charge.TopupKind}|{charge.Target}";
                    var prepayid = await this.wechatIntegration.Value.JSAPIPay(charge.Account,
                        charge.Amount,
                        charge.AppID,
                        charge.Device,
                        charge.Order,
                        charge.Product,
                        attach,
                        charge.IP,
                        branding.WechatMerchantID,
                        branding.WechatMerchantNotifyUri,
                        branding.WechatMerchantSignature);
                    if (!prepayid.IsSuccess)
                    {
                        this.logger.LogError($"支付通道上游返回错误: {prepayid.Message} 参考: {prepayid.Reference}");
                        continue;
                    }

                    var paySignature = this.wechatIntegration.Value.GetJSAPIPaySignature(prepayid.Result, charge.AppID, branding.WechatMerchantSignature);
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

        public Task<IEnumerable<Charge>> CreateOrUpdateAsync(IEnumerable<Charge> entities) => throw new NotImplementedException();
        public Task<IEnumerable<Charge>> DeleteAsync(IEnumerable<Charge> entities, bool successIfNotExist = false) => throw new NotImplementedException();
        public Task<TableQueryResult<Charge>> GetAsync(int limit) => throw new NotImplementedException();
        public Task<TableQueryResult<Charge>> GetAsync(string partitionKey, int limit) => throw new NotImplementedException();
        public Task<Charge> GetAsync(string partitionKey, string rowKey) => throw new NotImplementedException();
        public Task<IEnumerable<Charge>> UpdateAsync(IEnumerable<Charge> entities) => throw new NotImplementedException();
    }
}
