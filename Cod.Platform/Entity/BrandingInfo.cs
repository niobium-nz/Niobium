using Microsoft.WindowsAzure.Storage.Table;

namespace Cod.Platform
{
    public class BrandingInfo : TableEntity, IEntity
    {
        public string Name { get; set; }

        public string ApiUri { get; set; }

        public string WechatUri { get; set; }

        public string WechatAppID { get; set; }

        public string WechatSecret { get; set; }

        public string WechatMerchantID { get; set; }

        public string WechatDeliveryNotificationTemplateID { get; set; }

        public string WechatCollectedNotificationTemplateID { get; set; }

        public string WechatAbnormalStatusNotificationTemplateID { get; set; }

        public string WechatDeviceFaultNotificationTemplateID { get; set; }

        public string WechatOrderStatusNotificationTemplateID { get; set; }

        public string WechatMerchantSignature { get; set; }

        public DateTimeOffset? Created { get; set; }

        public static string BuildPartitionKey(string brand) => brand.Trim().ToUpperInvariant();
    }
}