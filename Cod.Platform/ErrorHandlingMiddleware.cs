using Microsoft.AspNetCore.Http;
using System.Net;
using System.Text.Json;

namespace Cod.Platform
{
    internal class ErrorHandlingMiddleware : IMiddleware
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
                if (ex.ErrorCode >= 100 && ex.ErrorCode <= 999)
                {
                    context.Response.StatusCode = ex.ErrorCode;
                }
                else
                {
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                }

                if (ex.ErrorCode == (int)HttpStatusCode.BadRequest && ex.Reference is ValidationState validation)
                {
                    await context.Response.WriteAsJsonAsync(new ValidationErrorPayload
                    {
                        Code = ex.ErrorCode,
                        Message = ex.Message,
                        Validation = validation,
                    }, serializationOptions, responseContentType, context.RequestAborted);
                }
                else
                {
                    await context.Response.WriteAsJsonAsync(new GenericErrorPayload
                    {
                        Code = ex.ErrorCode,
                        Message = ex.Message,
                        Reference = ex.Reference,
                    }, serializationOptions, responseContentType, context.RequestAborted);
                }
            }
        }
    }
}
