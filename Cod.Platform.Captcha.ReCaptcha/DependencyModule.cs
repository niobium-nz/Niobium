using Cod.Platform.Captcha.Recaptcha;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cod.Platform.Captcha
{
    public static class DependencyModule
    {
        private const string recaptchaHost = "https://www.google.com/";
        private static volatile bool loaded;

        public static IServiceCollection AddCaptcha(this IServiceCollection services, IConfiguration configuration)
        {
            return services.AddCaptcha(configuration.Bind);
        }

        public static IServiceCollection AddCaptcha(
            this IServiceCollection services,
            Action<CaptchaOptions> options)
        {
            if (loaded)
            {
                return services;
            }

            loaded = true;

            services.AddPlatform();

            services.Configure<CaptchaOptions>(o => options(o));

            services.AddTransient<IVisitorRiskAssessor, GoogleReCaptchaRiskAssessor>();
            services.AddHttpClient<IVisitorRiskAssessor, GoogleReCaptchaRiskAssessor>((sp, httpClient) =>
            {
                httpClient.BaseAddress = new Uri(recaptchaHost);
            })
            .AddStandardResilienceHandler();
            return services;
        }
    }
}