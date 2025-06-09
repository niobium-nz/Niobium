using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using System.Net;

namespace Cod.Platform.Finance
{
    internal class PaymentRequestMiddleware : IMiddleware
    {
        public const string PaymentUserQueryParameter = "user";
        public const string PaymentOrderQueryParameter = "order";
        public const string PaymentCurrencyQueryParameter = "currency";
        public const string PaymentAmountQueryParameter = "amount";
        private readonly Lazy<IPaymentProcessor> paymentProcessor;
        private readonly IOptions<PaymentServiceOptions> options;

        public PaymentRequestMiddleware(Lazy<IPaymentProcessor> paymentProcessor, IOptions<PaymentServiceOptions> options)
        {
            this.paymentProcessor = paymentProcessor;
            this.options = options;
        }

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

            if (!req.Query.TryGetValue(PaymentUserQueryParameter, out var u) || !Guid.TryParse(u, out var user))
            {
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                await context.Response.WriteAsync($"Invalid '{PaymentUserQueryParameter}' query parameter.");
                return;
            }

            string order = null;
            if (req.Query.TryGetValue(PaymentOrderQueryParameter, out var o) )
            {
                order = o.SingleOrDefault();
                if (!string.IsNullOrWhiteSpace(order))
                {
                    order = order.Trim();
                }
            }

            if (!req.Query.TryGetValue(PaymentCurrencyQueryParameter, out var c) || !Currency.TryParse(c, out var currency))
            {
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                await context.Response.WriteAsync($"Invalid '{PaymentCurrencyQueryParameter}' query parameter.");
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
                Target = user.ToString(),
                Channel = PaymentChannels.Cards,
                Operation = PaymentOperationKind.Charge,
                Source = req.Headers.Origin,
                Order = order,
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
