using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.OpenTelemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Niobium.Platform.Functions
{
    public static class DependencyModule
    {
        private static volatile bool added;
        private static volatile bool used;

        public static TBuilder AddPlatform<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
        {
            if (added)
            {
                return builder;
            }

            added = true;
            builder.AddServiceDefaults();

            if (!String.IsNullOrEmpty(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
            {
                builder.Services.AddOpenTelemetry()
                    .UseFunctionsWorkerDefaults();
            }

            builder.Services.AddPlatform();
            builder.Services.AddSingleton<IHttpContextAccessor, FunctionContextAccessor>();
            builder.Services.AddTransient<FunctionMiddlewareAdaptor<ErrorHandlingMiddleware>>();
            return builder;
        }

        public static TBuilder UsePlatform<TBuilder>(this TBuilder builder) where TBuilder : IFunctionsWorkerApplicationBuilder
        {
            if (used)
            {
                return builder;
            }

            used = true;
            builder.UseMiddleware<FunctionContextAccessorMiddleware>();
            builder.ToMiddlewareHost().UsePlatform();
            return builder;
        }
    }
}