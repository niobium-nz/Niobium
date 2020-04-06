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
            Lazy<IRepository<OpenID>> repository,
            Lazy<IRepository<BrandingInfo>> brandingRepository,
            Lazy<WechatIntegration> wechatIntegration)
            : base(repository)
        {
            this.brandingRepository = brandingRepository;
            this.wechatIntegration = wechatIntegration;
        }

        protected override OpenIDProvider ProviderSupport => OpenIDProvider.Wechat;

        protected async override Task<OperationResult> SendPushAsync(
            string brand,
            IReadOnlyCollection<NotificationContext> targets,
            int template,
            IReadOnlyDictionary<string, object> parameters)
        {
            var brandcache = new Dictionary<string, BrandingInfo>();
            var result = true;
            foreach (var target in targets)
            {
                var cacheKey = $"{(int)target.Provider}|{target.AppID}";
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
