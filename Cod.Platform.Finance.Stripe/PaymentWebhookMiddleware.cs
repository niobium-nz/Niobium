using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using System.Net;
using System.Text.Json;

namespace Cod.Platform.Finance.Stripe
{
    internal class PaymentWebhookMiddleware(Lazy<IPaymentProcessor> paymentProcessor, IOptions<PaymentServiceOptions> options) : IMiddleware
    {
        private static readonly JsonSerializerOptions serializationOptions = new(JsonSerializerDefaults.Web);

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            var req = context.Request;
            if (!req.Path.HasValue || !req.Path.Value.Equals($"/{options.Value.PaymentWebHookEndpoint}", StringComparison.OrdinalIgnoreCase))
            {
                await next(context);
                return;
            }

            if (req.Method != HttpMethods.Post)
            {
                context.Response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                return;
            }

            if (!req.HasJsonContentType())
            {
                context.Response.StatusCode = (int)HttpStatusCode.UnsupportedMediaType;
                return;
            }

            var chargeRequest = await req.ReadFromJsonAsync<ChargeRequest>(serializationOptions, context.RequestAborted);
            var result = await paymentProcessor.Value.ChargeAsync(chargeRequest);
            var action = result.MakeResponse();
            await action.ExecuteResultAsync(new ActionContext(context, new RouteData(), new ActionDescriptor()));
        }
    }
}
