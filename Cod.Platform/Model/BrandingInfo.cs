namespace Cod.Platform.Model
{
    public class BrandingInfo
    {
        public string PartitionKey { get; set; }

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

        public string WechatMerchantSignature { get; set; }

        public string WechatMerchantNotifyUri { get; set; }

        public string SMSIntegrationKey { get; set; }

        public string SMSIntegrationSecret { get; set; }

        public static string BuildPartitionKey(string brand) => brand.Trim().ToUpperInvariant();
    }
}