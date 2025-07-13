using Cod.Profile;
using Microsoft.Extensions.DependencyInjection;

namespace Cod.Channel.Profile
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

            Cod.Profile.DependencyModule.AddProfile(services, options, testMode);
            services.AddTransient(typeof(IProfileService<>), typeof(ChannelProfileService<>));
            return services;
        }
    }
}
