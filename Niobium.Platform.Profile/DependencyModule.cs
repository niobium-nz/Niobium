using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Niobium.Profile;

namespace Niobium.Platform.Profile
{
    public static class DependencyModule
    {
        private static volatile bool loaded;

        public static void AddProfile(this IHostApplicationBuilder builder, bool useServicePrincipalAuthentication = false)
        {
            if (loaded)
            {
                return;
            }

            loaded = true;

            bool isDevelopment = builder.Environment.IsDevelopment();
            Niobium.Profile.DependencyModule.AddProfile(
                builder.Services,
                options => 
                {
                    builder.Configuration.GetSection(nameof(ProfileOptions)).Bind(options);
                    options.TestMode = isDevelopment;
                },
                isDevelopment);

            if (useServicePrincipalAuthentication)
            {
                builder.Services.AddTransient(typeof(IProfileService<>), typeof(ServicePrincipalProfileService<>));
            }
            else
            {
                builder.Services.AddTransient(typeof(IProfileService<>), typeof(ClientTokenProfileService<>));
            }
        }
    }
}