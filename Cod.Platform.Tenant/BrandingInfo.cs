namespace Cod.Platform.Tenant
{
    public class BrandingInfo : ITrackable
    {
        [EntityKey(EntityKeyKind.PartitionKey)]
        public string PartitionKey { get; set; }

        [EntityKey(EntityKeyKind.RowKey)]
        public string RowKey { get; set; }

        [EntityKey(EntityKeyKind.Timestamp)]
        public DateTimeOffset? Timestamp { get; set; }

        [EntityKey(EntityKeyKind.ETag)]
        public string ETag { get; set; }

        public DateTimeOffset? Created { get; set; }

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

        public static string BuildPartitionKey(string brand)
        {
            return brand.Trim().ToUpperInvariant();
        }
    }
}