using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;

namespace Cod.Platform
{
    public class HttpResponseMessageResult : IActionResult
    {
        private readonly HttpResponseMessage responseMessage;

        public HttpResponseMessageResult(HttpResponseMessage responseMessage)
            => this.responseMessage = responseMessage ?? throw new System.ArgumentNullException(nameof(responseMessage));

        public async Task ExecuteResultAsync(ActionContext context)
        {
            context.HttpContext.Response.StatusCode = (int)this.responseMessage.StatusCode;

            foreach (var header in this.responseMessage.Headers)
            {
                context.HttpContext.Response.Headers.Add(header.Key, new StringValues(header.Value.ToArray()));
            }

            using (var stream = await this.responseMessage.Content.ReadAsStreamAsync())
            {
                await stream.CopyToAsync(context.HttpContext.Response.Body);
                await context.HttpContext.Response.Body.FlushAsync();
            }
        }
    }
}
