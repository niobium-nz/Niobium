using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging;
using Niobium.Platform;

namespace Niobium.Platform.Tests
{
    [TestClass]
    public sealed class HttpClientRedirectTests
    {
        private static HttpMessageHandler Primary(IServiceProvider services, string name)
        {
            HttpMessageHandler handler = services.GetRequiredService<IHttpMessageHandlerFactory>().CreateHandler(name);
            while (handler is DelegatingHandler wrapper)
            {
                handler = wrapper.InnerHandler!;
            }
            return handler;
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void SocketsTransport_PreservesHandlerAndConnectionSettings(bool enabled)
        {
            using SocketsHttpHandler transport = new()
            {
                UseCookies = false,
                UseProxy = false,
                PooledConnectionLifetime = TimeSpan.FromMinutes(7),
                MaxAutomaticRedirections = 3,
                AllowAutoRedirect = !enabled,
            };
            ServiceCollection services = new();
            services.AddHttpClient("configured")
                .ConfigurePrimaryHttpMessageHandler(() => transport)
                .ConfigureAutoRedirect(enabled);
            using ServiceProvider provider = services.BuildServiceProvider();

            Assert.AreSame(transport, Primary(provider, "configured"));
            Assert.AreEqual(enabled, transport.AllowAutoRedirect);
            Assert.IsFalse(transport.UseCookies);
            Assert.IsFalse(transport.UseProxy);
            Assert.AreEqual(TimeSpan.FromMinutes(7), transport.PooledConnectionLifetime);
            Assert.AreEqual(3, transport.MaxAutomaticRedirections);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void LegacyTransport_PreservesHandlerAndCookieSettings(bool enabled)
        {
            using HttpClientHandler transport = new()
            {
                UseCookies = false,
                MaxAutomaticRedirections = 3,
                AllowAutoRedirect = !enabled,
            };
            ServiceCollection services = new();
            services.AddHttpClient("configured")
                .ConfigurePrimaryHttpMessageHandler(() => transport)
                .ConfigureAutoRedirect(enabled);
            using ServiceProvider provider = services.BuildServiceProvider();

            Assert.AreSame(transport, Primary(provider, "configured"));
            Assert.AreEqual(enabled, transport.AllowAutoRedirect);
            Assert.IsFalse(transport.UseCookies);
            Assert.AreEqual(3, transport.MaxAutomaticRedirections);
        }

        [TestMethod]
        public void NamedPolicy_PreservesFactoryLifetimeAndOtherClients()
        {
            ServiceCollection services = new();
            services.AddHttpClient("configured")
                .SetHandlerLifetime(TimeSpan.FromMinutes(7))
                .ConfigureAutoRedirect(false);
            services.AddHttpClient("unchanged");
            using ServiceProvider provider = services.BuildServiceProvider();

            SocketsHttpHandler configured = (SocketsHttpHandler)Primary(provider, "configured");
            Assert.IsFalse(configured.AllowAutoRedirect);
            Assert.AreEqual(TimeSpan.FromMinutes(7), configured.PooledConnectionLifetime);
            Assert.IsTrue(((SocketsHttpHandler)Primary(provider, "unchanged")).AllowAutoRedirect);
        }

        [TestMethod]
        public void SharedDefault_AllowsAnExplicitClientOverride()
        {
            ServiceCollection services = new();
            services.ConfigureHttpClientDefaults(http => http.ConfigureAutoRedirect(false));
            services.AddHttpClient("default");
            services.AddHttpClient("override").ConfigureAutoRedirect(true);
            using ServiceProvider provider = services.BuildServiceProvider();

            Assert.IsFalse(((SocketsHttpHandler)Primary(provider, "default")).AllowAutoRedirect);
            Assert.IsTrue(((SocketsHttpHandler)Primary(provider, "override")).AllowAutoRedirect);
        }

        [TestMethod]
        public void RedirectPolicy_PreservesExistingResiliencePipeline()
        {
            ServiceCollection services = new();
            services.ConfigureHttpClientDefaults(http => http.AddStandardResilienceHandler());
            services.AddHttpClient("configured").ConfigureAutoRedirect(false);
            using ServiceProvider provider = services.BuildServiceProvider();
            HttpMessageHandler handler = provider.GetRequiredService<IHttpMessageHandlerFactory>().CreateHandler("configured");
            bool hasResilience = false;
            while (handler is DelegatingHandler wrapper)
            {
                hasResilience |= wrapper is ResilienceHandler;
                handler = wrapper.InnerHandler!;
            }

            Assert.IsTrue(hasResilience);
            Assert.IsFalse(((SocketsHttpHandler)handler).AllowAutoRedirect);
        }

        private sealed class UnsupportedHandler : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
                => throw new AssertFailedException("No network request is expected.");
        }

        [TestMethod]
        public void UnsupportedTransport_FailsDuringClientCreation()
        {
            ServiceCollection services = new();
            services.AddHttpClient("unsupported")
                .ConfigurePrimaryHttpMessageHandler(() => new UnsupportedHandler())
                .ConfigureAutoRedirect(false);
            using ServiceProvider provider = services.BuildServiceProvider();

            Assert.ThrowsExactly<InvalidOperationException>(() =>
                provider.GetRequiredService<IHttpClientFactory>().CreateClient("unsupported"));
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task RedirectedPost_ReachesDestinationOnlyWhenEnabled(bool enabled)
        {
            WebApplicationBuilder host = WebApplication.CreateSlimBuilder();
            host.WebHost.UseKestrel().UseUrls("http://127.0.0.1:0");
            host.Logging.ClearProviders();
            await using WebApplication app = host.Build();
            int initialRequests = 0;
            int destinationRequests = 0;
            string? forwardedBody = null;
            app.MapPost("/start", context =>
            {
                Interlocked.Increment(ref initialRequests);
                context.Response.StatusCode = StatusCodes.Status307TemporaryRedirect;
                context.Response.Headers.Location = "/finish";
                return Task.CompletedTask;
            });
            app.MapPost("/finish", async context =>
            {
                Interlocked.Increment(ref destinationRequests);
                using StreamReader reader = new(context.Request.Body);
                forwardedBody = await reader.ReadToEndAsync(context.RequestAborted);
                context.Response.StatusCode = StatusCodes.Status202Accepted;
            });
            using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(15));
            await app.StartAsync(timeout.Token);
            try
            {
                ServiceCollection services = new();
                services.AddHttpClient("sender").ConfigureAutoRedirect(enabled);
                using ServiceProvider provider = services.BuildServiceProvider();
                using HttpClient client = provider.GetRequiredService<IHttpClientFactory>().CreateClient("sender");
                using StringContent body = new("synthetic notification");
                using HttpResponseMessage response = await client.PostAsync(new Uri(new Uri(app.Urls.Single()), "/start"), body, timeout.Token);

                Assert.AreEqual(enabled ? HttpStatusCode.Accepted : HttpStatusCode.TemporaryRedirect, response.StatusCode);
                Assert.AreEqual(1, initialRequests);
                Assert.AreEqual(enabled ? 1 : 0, destinationRequests);
                Assert.AreEqual(enabled ? "synthetic notification" : null, forwardedBody);
            }
            finally
            {
                await app.StopAsync(CancellationToken.None);
            }
        }
    }
}