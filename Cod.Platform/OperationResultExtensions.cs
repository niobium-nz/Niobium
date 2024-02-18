using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace Cod.Platform
{
    public static class OperationResultExtensions
    {
        public static IActionResult MakeResponse<T>(this OperationResult<T> operationResult, JsonSerializationFormat? serializationFormat = null)
        {
            if (operationResult is null)
            {
                throw new ArgumentNullException(nameof(operationResult));
            }

            object payload = operationResult;
            HttpStatusCode? code;
            if (operationResult.IsSuccess)
            {
                code = HttpStatusCode.OK;
                payload = operationResult.Result;
            }
            else
            {
                code = operationResult.Code < 600 ? (HttpStatusCode)operationResult.Code : HttpStatusCode.InternalServerError;
            }

            return HttpRequestExtensions.MakeResponse(null, statusCode: code, payload: payload, serializationFormat: serializationFormat);
        }

        public static IActionResult MakeResponse(this OperationResult operationResult, object successPayload = null, JsonSerializationFormat? serializationFormat = null)
        {
            if (operationResult is null)
            {
                throw new ArgumentNullException(nameof(operationResult));
            }

            object payload = operationResult;
            HttpStatusCode? code;
            if (operationResult.IsSuccess)
            {
                code = HttpStatusCode.OK;
                payload = successPayload;
            }
            else if (operationResult.Code == 400)
            {
                code = HttpStatusCode.BadRequest;
                payload = operationResult.Reference;
            }
            else
            {
                code = operationResult.Code < 600 ? (HttpStatusCode)operationResult.Code : HttpStatusCode.InternalServerError;
            }

            return HttpRequestExtensions.MakeResponse(null, statusCode: code, payload: payload, serializationFormat: serializationFormat);
        }
    }
}
