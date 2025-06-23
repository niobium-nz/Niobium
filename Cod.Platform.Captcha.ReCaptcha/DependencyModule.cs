using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Cod.Platform.Captcha.ReCaptcha
{
    public static class DependencyModule
    {
        private static volatile bool loaded;

        public static void AddCaptcha(this IHostApplicationBuilder builder)
        {
            if (loaded)
            {
                return;
            }

            builder.Services.AddCaptcha(builder.Configuration.GetSection(nameof(CaptchaOptions)).Bind);

            if (builder.Configuration.IsDevelopmentEnvironment())
            {
                builder.Services.AddTransient<IVisitorRiskAssessor, DevelopmentRiskAccessor>();
            }
        }

        public static IServiceCollection AddCaptcha(this IServiceCollection services, Action<CaptchaOptions>? options)
        {
            if (loaded)
            {
                return services;
            }

            loaded = true;

            services.AddPlatform();

            services.Configure<CaptchaOptions>(o => options?.Invoke(o));
            services.AddTransient<DevelopmentRiskAccessor>();
            services.AddTransient<GoogleReCaptchaRiskAssessor>();

            services.AddHttpClient<IVisitorRiskAssessor, GoogleReCaptchaRiskAssessor>(new Func<HttpClient, IServiceProvider, GoogleReCaptchaRiskAssessor>(
                (httpClient, sp) =>
                {
                    var captchaOptions = sp.GetRequiredService<IOptions<CaptchaOptions>>().Value;
                    if (captchaOptions.IsDisabled)
                    {
                        return sp.GetRequiredService<DevelopmentRiskAccessor>();
                    }
                    else
                    {
                        return sp.GetRequiredService<GoogleReCaptchaRiskAssessor>();
                    }
                }))
            .AddStandardResilienceHandler();

            return services;
        }
    }
}