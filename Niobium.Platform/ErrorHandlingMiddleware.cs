using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace Niobium.Platform
{
    internal sealed class ErrorHandlingMiddleware(ILogger<ErrorHandlingMiddleware> logger) : IMiddleware
    {
        private const string responseContentType = "application/json";
        private static readonly JsonSerializerOptions serializationOptions = new(JsonSerializerDefaults.Web);

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            try
            {
                await next(context);
            }
            catch (ApplicationException ex)
            {
                context.Response.StatusCode = ex.ErrorCode is >= 100 and <= 999 ? ex.ErrorCode : (int)HttpStatusCode.InternalServerError;

                object payload = ex.ErrorCode == (int)HttpStatusCode.BadRequest && ex.Reference is ValidationState validation
                    ? new ValidationErrorPayload
                    {
                        Code = ex.ErrorCode,
                        Message = ex.Message,
                        Validation = validation,
                    }
                    : (object)new GenericErrorPayload
                    {
                        Code = ex.ErrorCode,
                        Message = ex.Message,
                        Reference = ex.Reference,
                    };
                await context.Response.WriteAsJsonAsync(payload, serializationOptions, responseContentType, context.RequestAborted);
                logger.LogError(ex, $"Error handling request: {ex.Message}", payload);
            }
        }
    }
}
