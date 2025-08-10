using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using System.Net;

namespace Cod.Platform.Finance
{
    internal sealed class PaymentWebhookMiddleware(IPaymentService paymentService, IOptions<PaymentServiceOptions> options)
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

            using StreamReader reader = new(req.Body);
            string json = await reader.ReadToEndAsync();
            OperationResult<Cod.Finance.ChargeResult> result = await paymentService.ReportAsync(json);
            IActionResult action = result.MakeResponse();
            await action.ExecuteResultAsync(new ActionContext(context, new RouteData(), new ActionDescriptor()));
        }
    }
}
