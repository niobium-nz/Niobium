namespace Cod.Platform
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
            if (level != (int)OpenIDKind.Wechat)
            {
                return OperationResult.NotAllowed;
            }
            return await base.SendAsync(brand, user, context, templateID, parameters, level);
        }

        protected override async Task<OperationResult> SendPushAsync(
            string brand,
            IEnumerable<NotificationContext> targets,
            int templateID,
            IReadOnlyDictionary<string, object> parameters)
        {
            var result = false;
            foreach (var target in targets)
            {
                var cacheKey = $"{target.Kind}|{target.App}";
                var brandingInfo = await this.brandManager.Value.GetAsync(brand);

                if (brandingInfo == null)
                {
                    return OperationResult.InternalServerError;
                }

                if (brandingInfo.WechatAppID != target.App)
                {
                    //REMARK (5he11) 当前推送的上下文所基于的品牌与推送目标的AppID不一致时应该跳过
                    continue;
                }

                var tid = await this.GetTemplateIDAsync(brandingInfo, target, templateID, parameters);
                var parameter = await this.GetTemplateParameterAsync(brandingInfo, target, templateID, parameters);
                var link = await this.GetTemplateLinkAsync(brandingInfo, target, templateID, parameters);
                var notificationResult = await this.wechatIntegration.Value.SendNotificationAsync(
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

            if (result)
            {
                return OperationResult.Success;
            }
            else
            {
                return OperationResult.InternalServerError;
            }
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
