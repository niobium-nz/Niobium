using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cod.Platform.Model;

namespace Cod.Platform
{
    public abstract class WechatPushNotificationChannel : PushNotificationChannel
    {
        private readonly Lazy<IRepository<BrandingInfo>> brandingRepository;
        private readonly Lazy<WechatIntegration> wechatIntegration;

        public WechatPushNotificationChannel(
            Lazy<IOpenIDManager> openIDManager,
            Lazy<IRepository<BrandingInfo>> brandingRepository,
            Lazy<WechatIntegration> wechatIntegration)
            : base(openIDManager)
        {
            this.brandingRepository = brandingRepository;
            this.wechatIntegration = wechatIntegration;
        }

        public async override Task<OperationResult> SendAsync(
            string brand,
            string account,
            NotificationContext context,
            int template,
            IReadOnlyDictionary<string, object> parameters,
            int level = 0)
        {
            if (level != (int)OpenIDKind.Wechat)
            {
                return OperationResult.Create(InternalError.NotAllowed);
            }
            return await base.SendAsync(brand, account, context, template, parameters, level);
        }

        protected async override Task<OperationResult> SendPushAsync(
            string brand,
            IEnumerable<NotificationContext> targets,
            int template,
            IReadOnlyDictionary<string, object> parameters)
        {
            var brandcache = new Dictionary<string, BrandingInfo>();
            var result = true;
            foreach (var target in targets)
            {
                var cacheKey = $"{target.Kind}|{target.AppID}";
                BrandingInfo brandingInfo;
                if (brandcache.ContainsKey(cacheKey))
                {
                    brandingInfo = brandcache[cacheKey];
                }
                else
                {
                    brandingInfo = await this.brandingRepository.Value.GetByBrandAsync(brand);
                }

                if (brandingInfo == null)
                {
                    return OperationResult.Create(InternalError.InternalServerError);
                }
                else
                {
                    brandcache.Add(cacheKey, brandingInfo);
                }

                var templateID = await this.GetTemplateIDAsync(brandingInfo, target, template, parameters);
                var parameter = await this.GetTemplateParameterAsync(brandingInfo, target, template, parameters);
                var link = await this.GetTemplateLinkAsync(brandingInfo, target, template, parameters);
                var notificationResult = await this.wechatIntegration.Value.SendNotificationAsync(
                    target.AppID,
                    brandingInfo.WechatSecret,
                    target.UserID,
                    templateID,
                    parameter,
                    link);


                if (!notificationResult.IsSuccess)
                {
                    result = false;
                }
            }

            if (result)
            {
                return OperationResult.Create();
            }
            else
            {
                return OperationResult.Create(InternalError.InternalServerError);
            }
        }

        protected abstract Task<string> GetTemplateIDAsync(
            BrandingInfo brand,
            NotificationContext target,
            int template,
            IReadOnlyDictionary<string, object> parameters);

        protected abstract Task<WechatNotificationParameter> GetTemplateParameterAsync(
            BrandingInfo brand,
            NotificationContext target,
            int template,
            IReadOnlyDictionary<string, object> parameters);

        protected abstract Task<string> GetTemplateLinkAsync(
            BrandingInfo brand,
            NotificationContext target,
            int template,
            IReadOnlyDictionary<string, object> parameters);
    }
}
