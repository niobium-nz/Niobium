using System;
using System.Net;
using Microsoft.AspNetCore.Mvc;

namespace Cod.Platform
{
    public static class ValidationStateExtensions
    {
        public static IActionResult MakeResponse(this ValidationState validationState)
        {
            if (validationState is null)
            {
                throw new ArgumentNullException(nameof(validationState));
            }

            return HttpRequestExtensions.MakeResponse(
                null,
                statusCode: HttpStatusCode.BadRequest,
                payload: validationState.ToDictionary());
        }
    }
}
