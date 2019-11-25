using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Cod.Platform.Charges
{
    internal class ChargeNotificationRepository : IRepository<ChargeNotification>
    {
        private readonly Lazy<IRepository<BrandingInfo>> brandingRepository;
        private readonly Lazy<IConfigurationProvider> configuration;
        private readonly ILogger logger;

        public ChargeNotificationRepository(Lazy<IRepository<BrandingInfo>> brandingRepository,
            Lazy<IConfigurationProvider> configuration,
            ILogger logger)
        {
            this.brandingRepository = brandingRepository;
            this.configuration = configuration;
            this.logger = logger;
        }

        public async Task<IEnumerable<ChargeNotification>> CreateAsync(IEnumerable<ChargeNotification> entities, bool replaceIfExist)
        {
            var notifies = new List<ChargeNotification>();
            var key = await this.configuration.Value.GetSettingAsync("CHARGE_SECRET");
            foreach (var notify in entities)
            {
                if (notify is WechatChargeNotification wechatCharge)
                {
                    var brandings = await this.brandingRepository.Value.GetAsync();
                    var branding = brandings.SingleOrDefault(b => b.WechatAppID == wechatCharge.AppID);
                    if (branding != null &&
                        WechatHelper.ValidateNotification(wechatCharge, branding.WechatMerchantSignature, key, this.logger))
                    {
                        notifies.Add(notify);
                    }
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
            return notifies;
        }

        public Task<IEnumerable<ChargeNotification>> DeleteAsync(IEnumerable<ChargeNotification> entities) => throw new NotImplementedException();
        public Task<TableQueryResult<ChargeNotification>> GetAsync(int limit) => throw new NotImplementedException();
        public Task<TableQueryResult<ChargeNotification>> GetAsync(string partitionKey, int limit) => throw new NotImplementedException();
        public Task<ChargeNotification> GetAsync(string partitionKey, string rowKey) => throw new NotImplementedException();
        public Task<IEnumerable<ChargeNotification>> UpdateAsync(IEnumerable<ChargeNotification> entities) => throw new NotImplementedException();
    }
}
