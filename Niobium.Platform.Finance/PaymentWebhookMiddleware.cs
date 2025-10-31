using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Niobium.Finance;
using System.Net;

namespace Niobium.Platform.Finance
{
    internal sealed class PaymentWebhookMiddleware(IPaymentService paymentService, IOptions<PaymentServiceOptions> options, ILogger<PaymentWebhookMiddleware> logger)
        : IMiddleware
    {
        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            HttpRequest req = context.Request;
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

            if (!req.Query.TryGetValue(PaymentRequestMiddleware.PaymentTenantQueryParameter, out Microsoft.Extensions.Primitives.StringValues t))
            {
                logger.LogWarning("Payment webhook request missing tenant query parameter.");
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                await context.Response.WriteAsync($"Invalid '{PaymentRequestMiddleware.PaymentTenantQueryParameter}' query parameter.");
                return;
            }
            var tenant = t.First()!;

            using StreamReader reader = new(req.Body);
            string json = await reader.ReadToEndAsync();
            OperationResult<ChargeResult> result = await paymentService.ReportAsync(tenant, json);
            IActionResult action = result.MakeResponse(JsonMarshallingFormat.CamelCase);
            await action.ExecuteResultAsync(new ActionContext(context, new RouteData(), new ActionDescriptor()));
        }
    }
}
