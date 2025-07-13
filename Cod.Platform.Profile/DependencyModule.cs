using Cod.Profile;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cod.Platform.Profile
{
    public static class DependencyModule
    {
        private static volatile bool loaded;
        private static bool isDevelopment;

        public static void AddProfile(this IHostApplicationBuilder builder)
        {
            isDevelopment = builder.Environment.IsDevelopment();
            builder.Services.AddProfile(builder.Configuration.GetSection(nameof(ProfileOptions)).Bind);
        }

        public static IServiceCollection AddProfile(
            this IServiceCollection services,
            Action<ProfileOptions>? options)
        {
            if (loaded)
            {
                return services;
            }

            loaded = true;

            Cod.Profile.DependencyModule.AddProfile(services, options, isDevelopment);
            services.AddTransient(typeof(IProfileService<>), typeof(PlatformProfileService<>));
            return services;
        }
    }
}