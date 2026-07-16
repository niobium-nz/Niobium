using Microsoft.Extensions.Configuration;

namespace Niobium.Platform
{
    public static class IConfigurationExtensions
    {
        public static bool IsPreProductionEnvironment(this IConfiguration configuration)
        {
            return configuration.IsDevelopmentEnvironment() || configuration.IsStagingEnvironment();
        }

        public static bool IsDevelopmentEnvironment(this IConfiguration configuration)
        {
            return configuration.GetServiceEnvironment() == Constants.DevelopmentEnvironment;
        }

        public static bool IsStagingEnvironment(this IConfiguration configuration)
        {
            return configuration.GetServiceEnvironment() == Constants.StagingEnvironment;
        }

        public static bool IsProductionEnvironment(this IConfiguration configuration)
        {
            return configuration.GetServiceEnvironment() == Constants.ProductionEnvironment;
        }

        private static string? GetServiceEnvironment(this IConfiguration configuration)
          => configuration.GetValue<string>(Constants.ServiceEnvironment)
            ?? configuration.GetValue<string>(Constants.AspNetCoreEnvironment);
    }
}
