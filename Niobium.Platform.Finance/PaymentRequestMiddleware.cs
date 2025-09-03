using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using Niobium.Finance;
using System.Net;

namespace Niobium.Platform.Finance
{
    internal sealed class PaymentRequestMiddleware(IPaymentService paymentService, IOptions<PaymentServiceOptions> options)
        : IMiddleware
    {
        public const string PaymentUserQueryParameter = "user";
        public const string PaymentOrderQueryParameter = "order";
        public const string PaymentCurrencyQueryParameter = "currency";
        public const string PaymentAmountQueryParameter = "amount";

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            HttpRequest req = context.Request;
            if (!req.Path.HasValue || !req.Path.Value.Equals($"/{options.Value.PaymentRequestEndpoint}", StringComparison.OrdinalIgnoreCase))
            {
                await next(context);
                return;
            }

            if (req.Method != HttpMethods.Get)
            {
                context.Response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                return;
            }

            if (!req.Query.TryGetValue(PaymentUserQueryParameter, out Microsoft.Extensions.Primitives.StringValues u) || !Guid.TryParse(u, out Guid user))
            {
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                await context.Response.WriteAsync($"Invalid '{PaymentUserQueryParameter}' query parameter.");
                return;
            }

            string? order = null;
            if (req.Query.TryGetValue(PaymentOrderQueryParameter, out Microsoft.Extensions.Primitives.StringValues o))
            {
                order = o.SingleOrDefault();
                if (!string.IsNullOrWhiteSpace(order))
                {
                    order = order.Trim();
                }
            }

            if (!req.Query.TryGetValue(PaymentCurrencyQueryParameter, out Microsoft.Extensions.Primitives.StringValues c) || !Currency.TryParse(c!, out Currency currency))
            {
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                await context.Response.WriteAsync($"Invalid '{PaymentCurrencyQueryParameter}' query parameter.");
                return;
            }

            if (!req.Query.TryGetValue(PaymentAmountQueryParameter, out Microsoft.Extensions.Primitives.StringValues a) || !long.TryParse(a, out long amount) || amount <= 0 || amount > 100000)
            {
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                await context.Response.WriteAsync($"Invalid '{PaymentAmountQueryParameter}' query parameter.");
                return;
            }

            ChargeRequest chargeRequest = new()
            {
                TargetKind = ChargeTargetKind.User,
                Target = user.ToString(),
                Channel = PaymentChannels.Cards,
                Operation = PaymentOperationKind.Charge,
                Tenant = req.GetSourceHostname(),
                Order = order,
                Amount = amount,
                Currency = currency,
                IP = req.GetRemoteIP(),
            };
            OperationResult<ChargeResponse> result = await paymentService.ChargeAsync(chargeRequest);
            IActionResult action = result.MakeResponse(JsonMarshallingFormat.CamelCase);
            await action.ExecuteResultAsync(new ActionContext(context, new RouteData(), new ActionDescriptor()));
        }
    }
}
