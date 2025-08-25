using Microsoft.Extensions.DependencyInjection;
using Niobium.Profile;

namespace Niobium.Channel.Profile
{
    public static class DependencyModule
    {
        private static volatile bool loaded;

        public static IServiceCollection AddProfile(this IServiceCollection services, Action<ProfileOptions> options, bool testMode = false)
        {
            if (loaded)
            {
                return services;
            }

            loaded = true;

            Niobium.Profile.DependencyModule.AddProfile(services, options, testMode);
            services.AddTransient(typeof(IProfileService<>), typeof(ChannelProfileService<>));
            return services;
        }
    }
}
