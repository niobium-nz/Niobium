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
            return Constants.DevelopmentEnvironment.Equals(configuration.GetServiceEnvironment(), StringComparison.OrdinalIgnoreCase)
                || Constants.DevEnvironment.Equals(configuration.GetServiceEnvironment(), StringComparison.OrdinalIgnoreCase)
                || Constants.LocalEnvironment.Equals(configuration.GetServiceEnvironment(), StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsStagingEnvironment(this IConfiguration configuration)
        {
            return Constants.StagingEnvironment.Equals(configuration.GetServiceEnvironment(), StringComparison.OrdinalIgnoreCase)
                || Constants.TestEnvironment.Equals(configuration.GetServiceEnvironment(), StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsProductionEnvironment(this IConfiguration configuration)
        {
            return Constants.ProductionEnvironment.Equals(configuration.GetServiceEnvironment(), StringComparison.OrdinalIgnoreCase)
                || Constants.ProdEnvironment.Equals(configuration.GetServiceEnvironment(), StringComparison.OrdinalIgnoreCase);
        }

        private static string? GetServiceEnvironment(this IConfiguration configuration)
          => configuration.GetValue<string>(Constants.ServiceEnvironment)
            ?? configuration.GetValue<string>(Constants.AspNetCoreEnvironment);
    }
}
