using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace Cod.Platform
{
    internal class ErrorHandlingMiddleware : IMiddleware
    {
        private const string responseContentType = "application/json";
        private static readonly JsonSerializerOptions serializationOptions = new(JsonSerializerDefaults.Web);
        private readonly ILogger<ErrorHandlingMiddleware> logger;

        public ErrorHandlingMiddleware(ILogger<ErrorHandlingMiddleware> logger)
        {
            this.logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            try
            {
                await next(context);
            }
            catch (ApplicationException ex)
            {
                if (ex.ErrorCode >= 100 && ex.ErrorCode <= 999)
                {
                    context.Response.StatusCode = ex.ErrorCode;
                }
                else
                {
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                }

                object payload;
                if (ex.ErrorCode == (int)HttpStatusCode.BadRequest && ex.Reference is ValidationState validation)
                {
                    payload = new ValidationErrorPayload
                    {
                        Code = ex.ErrorCode,
                        Message = ex.Message,
                        Validation = validation,
                    };
                }
                else
                {
                    payload = new GenericErrorPayload
                    {
                        Code = ex.ErrorCode,
                        Message = ex.Message,
                        Reference = ex.Reference,
                    };
                }

                await context.Response.WriteAsJsonAsync(payload, serializationOptions, responseContentType, context.RequestAborted);
                logger.LogError(ex, $"Error handling request: {ex.Message}", payload);
            }
        }
    }
}
