using Cod.Platform.Identities;
using Cod.Platform.Tenants;
using Cod.Platform.Tenants.Wechat;

namespace Cod.Platform.Notification.Wechat
{
    public abstract class WechatPushNotificationChannel : PushNotificationChannel
    {
        private readonly Lazy<IBrandService> brandManager;
        private readonly Lazy<WechatIntegration> wechatIntegration;

        public WechatPushNotificationChannel(
            Lazy<IOpenIDManager> openIDManager,
            Lazy<IBrandService> brandManager,
            Lazy<WechatIntegration> wechatIntegration)
            : base(openIDManager)
        {
            this.brandManager = brandManager;
            this.wechatIntegration = wechatIntegration;
        }

        public override async Task<OperationResult> SendAsync(
            string brand,
            Guid user,
            NotificationContext context,
            int templateID,
            IReadOnlyDictionary<string, object> parameters,
            int level = 0)
        {
            return level != (int)OpenIDKind.Wechat
                ? OperationResult.NotAllowed
                : await base.SendAsync(brand, user, context, templateID, parameters, level);
        }

        protected override async Task<OperationResult> SendPushAsync(
            string brand,
            IEnumerable<NotificationContext> targets,
            int templateID,
            IReadOnlyDictionary<string, object> parameters)
        {
            bool result = false;
            foreach (NotificationContext target in targets)
            {
                string cacheKey = $"{target.Kind}|{target.App}";
                BrandingInfo brandingInfo = await brandManager.Value.GetAsync(brand);

                if (brandingInfo == null)
                {
                    return OperationResult.InternalServerError;
                }

                if (brandingInfo.WechatAppID != target.App)
                {
                    //REMARK (5he11) 当前推送的上下文所基于的品牌与推送目标的AppID不一致时应该跳过
                    continue;
                }

                string tid = await GetTemplateIDAsync(brandingInfo, target, templateID, parameters);
                WechatNotificationParameter parameter = await GetTemplateParameterAsync(brandingInfo, target, templateID, parameters);
                string link = await GetTemplateLinkAsync(brandingInfo, target, templateID, parameters);
                OperationResult<string> notificationResult = await wechatIntegration.Value.SendNotificationAsync(
                    target.App,
                    brandingInfo.WechatSecret,
                    target.Identity,
                    tid,
                    parameter,
                    link);


                if (notificationResult.IsSuccess)
                {
                    result = true;
                }
            }

            return result ? OperationResult.Success : OperationResult.InternalServerError;
        }

        protected abstract Task<string> GetTemplateIDAsync(
            BrandingInfo brand,
            NotificationContext target,
            int templateID,
            IReadOnlyDictionary<string, object> parameters);

        protected abstract Task<WechatNotificationParameter> GetTemplateParameterAsync(
            BrandingInfo brand,
            NotificationContext target,
            int templateID,
            IReadOnlyDictionary<string, object> parameters);

        protected abstract Task<string> GetTemplateLinkAsync(
            BrandingInfo brand,
            NotificationContext target,
            int templateID,
            IReadOnlyDictionary<string, object> parameters);
    }
}
