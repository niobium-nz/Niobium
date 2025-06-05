using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using System.Net;

namespace Cod.Platform.Finance.Stripe
{
    internal class PaymentRequestMiddleware(Lazy<IPaymentProcessor> paymentProcessor, IOptions<PaymentServiceOptions> options) : IMiddleware
    {
        public const string PaymentIDQueryParameter = "id";
        public const string PaymentCurrencyQueryParameter = "currency";
        public const string PaymentAmountQueryParameter = "amount";

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            var req = context.Request;
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

            if (!req.Query.TryGetValue(PaymentIDQueryParameter, out var id) || !Guid.TryParse(id, out var guid))
            {
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                await context.Response.WriteAsync($"Invalid '{PaymentIDQueryParameter}' query parameter.");
                return;
            }

            if (!req.Query.TryGetValue(PaymentCurrencyQueryParameter, out var c) || !Currency.TryParse(c, out var currency))
            {
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                await context.Response.WriteAsync($"Invalid '{PaymentIDQueryParameter}' query parameter.");
                return;
            }

            if (!req.Query.TryGetValue(PaymentAmountQueryParameter, out var a) || !long.TryParse(a, out var amount) || amount <= 0 || amount > 100000)
            {
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                await context.Response.WriteAsync($"Invalid '{PaymentAmountQueryParameter}' query parameter.");
                return;
            }

            var chargeRequest = new ChargeRequest
            {
                TargetKind = ChargeTargetKind.User,
                Target = guid.ToString(),
                Channel = PaymentChannels.Cards,
                Operation = PaymentOperationKind.Charge,
                Source = req.Headers.Origin,
                Order = guid,
                Amount = amount,
                Currency = currency,
                IP = req.GetRemoteIP(),
            };
            var result = await paymentProcessor.Value.ChargeAsync(chargeRequest);
            var action = result.MakeResponse(JsonSerializationFormat.CamelCase);
            await action.ExecuteResultAsync(new ActionContext(context, new RouteData(), new ActionDescriptor()));
        }
    }
}
