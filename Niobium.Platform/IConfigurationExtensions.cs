using Microsoft.Extensions.Configuration;

namespace Niobium.Platform
{
    public static class IConfigurationExtensions
    {
        public static bool IsPreProductionEnvironment(this IConfiguration configuration)
        {
            return configuration.IsDevelopmentEnvironment() || configuration.IsStagingEnvironment();
        }

        private static bool IsDevelopmentEnvironment(this IConfiguration configuration)
        {
            return configuration.GetValue<string>(Constants.ServiceEnvironment) == Constants.DevelopmentEnvironment;
        }

        private static bool IsStagingEnvironment(this IConfiguration configuration)
        {
            return configuration.GetValue<string>(Constants.ServiceEnvironment) == Constants.StagingEnvironment;
        }

        public static bool IsProductionEnvironment(this IConfiguration configuration)
        {
            return configuration.GetValue<string>(Constants.ServiceEnvironment) == Constants.ProductionEnvironment;
        }
    }
}
