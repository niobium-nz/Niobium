using System;
using System.Collections.Generic;
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

        protected override string CreateAliyunTemplate(int template)
            => template == NotificationTemplates.RegistrationVerification ? "SMS_172980267" : null;

        protected override string CreateAliyunTemplateParameter(int template, IReadOnlyDictionary<string, string> parameters)
            => parameters.ContainsKey(NotificationParameters.VerificationCode)
                ? REGISTRATION_SMS_PARAM.Replace("CODE", parameters[NotificationParameters.VerificationCode])
                : null;
    }
}
