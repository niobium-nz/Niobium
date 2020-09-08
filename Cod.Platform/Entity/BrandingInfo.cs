using System;
using Microsoft.Azure.Cosmos.Table;

namespace Cod.Platform
{
    public class BrandingInfo : TableEntity, IEntity
    {
        public string WechatUri { get; set; }

        public string FunctionUri { get; set; }

        public string QueueUri { get; set; }

        public string ChannelInstallationUri { get; set; }

        public string WechatAppID { get; set; }

        public string WechatSecret { get; set; }

        public string Name { get; set; }

        public string Domain { get; set; }

        public string WechatMerchantID { get; set; }

        public string WechatDeliveryNotificationTemplateID { get; set; }

        public string WechatCollectedNotificationTemplateID { get; set; }

        public string WechatAbnormalStatusNotificationTemplateID { get; set; }

        public string WechatDeviceFaultNotificationTemplateID { get; set; }

        public string WechatMerchantSignature { get; set; }

        public string WechatMerchantNotifyUri { get; set; }

        public string SMSIntegrationKey { get; set; }

        public string SMSIntegrationSecret { get; set; }

        public DateTimeOffset? Created { get; set; }

        public static string BuildPartitionKey(string brand) => brand.Trim().ToUpperInvariant();
    }
}