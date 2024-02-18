using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;

namespace Cod.Platform
{
    public class HttpResponseMessageResult : IActionResult
    {
        private readonly HttpResponseMessage responseMessage;

        public HttpResponseMessageResult(HttpResponseMessage responseMessage)
        {
            this.responseMessage = responseMessage ?? throw new System.ArgumentNullException(nameof(responseMessage));
        }

        public async Task ExecuteResultAsync(ActionContext context)
        {
            context.HttpContext.Response.StatusCode = (int)responseMessage.StatusCode;

            foreach (KeyValuePair<string, IEnumerable<string>> header in responseMessage.Headers)
            {
                context.HttpContext.Response.Headers.Add(header.Key, new StringValues(header.Value.ToArray()));
            }

            using Stream stream = await responseMessage.Content.ReadAsStreamAsync();
            await stream.CopyToAsync(context.HttpContext.Response.Body);
            await context.HttpContext.Response.Body.FlushAsync();
        }
    }
}
