using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace Cod.Platform
{
    public static class ValidationStateExtensions
    {
        public static IActionResult MakeResponse(this ValidationState validationState)
        {
            return validationState is null
                ? throw new ArgumentNullException(nameof(validationState))
                : HttpRequestExtensions.MakeResponse(
                null,
                statusCode: HttpStatusCode.BadRequest,
                payload: validationState.ToDictionary());
        }
    }
}
