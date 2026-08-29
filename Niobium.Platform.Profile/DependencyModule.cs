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

            Niobium.Profile.DependencyModule.AddProfile(builder);

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