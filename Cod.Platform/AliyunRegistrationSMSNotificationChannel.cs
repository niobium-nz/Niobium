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

        protected override Task<string> CreateAliyunTemplateAsync(int template)
            => Task.FromResult(template == NotificationTemplates.RegistrationVerification ? "SMS_172980267" : null);

        protected override Task<string> CreateAliyunTemplateParameterAsync(int template, IReadOnlyDictionary<string, string> parameters)
            => Task.FromResult(parameters.ContainsKey(NotificationParameters.VerificationCode)
                ? REGISTRATION_SMS_PARAM.Replace("CODE", parameters[NotificationParameters.VerificationCode])
                : null);
    }
}
