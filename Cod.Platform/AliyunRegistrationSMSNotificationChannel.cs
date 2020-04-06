using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cod.Platform.Model;
using Microsoft.Extensions.Logging;

namespace Cod.Platform
{
    internal class AliyunRegistrationSMSNotificationChannel : AliyunSMSNotificationChannel
    {
        private const string REGISTRATION_SMS_PARAM = "{\"code\":\"CODE\"}";

        public AliyunRegistrationSMSNotificationChannel(Lazy<IRepository<BrandingInfo>> repository, ILogger logger)
            : base(repository, logger)
        {
        }

        protected async override Task<OperationResult> SendSMSAsync(
            string brand,
            string mobile,
            int template,
            IReadOnlyDictionary<string, object> parameters)
        {
            if (template != NotificationTemplates.RegistrationVerification)
            {
                return OperationResult.Create(InternalError.NotAllowed);
            }

            if (!parameters.ContainsKey(NotificationParameters.VerificationCode))
            {
                return OperationResult.Create(InternalError.BadRequest);
            }

            return await base.SendSMSAsync(brand, mobile, template, parameters);
        }

        protected override Task<string> CreateAliyunTemplateAsync(int template)
            => Task.FromResult("SMS_172980267");

        protected override Task<string> CreateAliyunTemplateParameterAsync(int template, IReadOnlyDictionary<string, object> parameters)
            => Task.FromResult(REGISTRATION_SMS_PARAM.Replace("CODE", parameters.GetString(NotificationParameters.VerificationCode)));
    }
}
