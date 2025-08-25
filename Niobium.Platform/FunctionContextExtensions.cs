using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using System.Diagnostics.CodeAnalysis;

namespace Niobium.Platform
{
    /// <summary>
    /// Provides extension methods to work with <see cref="FunctionContext"/>.
    /// </summary>
    public static class FunctionContextExtensions
    {
        private const string HttpContextKey = "HttpRequestContext";

        /// <summary>
        /// Retrieve the <see cref="HttpContext"/> for the function execution.
        /// </summary>
        /// <param name="context">The <see cref="FunctionContext"/>.</param>
        /// <returns>The <see cref="HttpContext"/> for the function execution or null if it does not exist.</returns>
        public static HttpContext? GetHttpContext(this FunctionContext context)
        {
            return context.Items.TryGetValue(HttpContextKey, out object? requestContext)
                && requestContext is HttpContext httpContext
                ? httpContext
                : null;
        }

        /// <summary>
        /// Gets the <see cref="HttpRequest"/> for the <see cref="FunctionContext"/>.
        /// </summary>
        /// <param name="context">The <see cref="FunctionContext"/>.</param>
        /// <param name="request">The <see cref="HttpRequest"/> for the context.</param>
        /// <returns></returns>
        public static bool TryGetRequest(this FunctionContext context, [NotNullWhen(true)] out HttpRequest? request)
        {
            request = null;

            if (context.Items.TryGetValue(HttpContextKey, out object? requestContext)
                && requestContext is HttpContext httpContext)
            {
                request = httpContext.Request;
            }

            return request is not null;
        }
    }
}
