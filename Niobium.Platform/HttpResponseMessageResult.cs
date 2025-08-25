using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;

namespace Niobium.Platform
{
    public class HttpResponseMessageResult(HttpResponseMessage responseMessage) : IActionResult
    {
        public async Task ExecuteResultAsync(ActionContext context)
        {
            context.HttpContext.Response.StatusCode = (int)responseMessage.StatusCode;

            foreach (KeyValuePair<string, IEnumerable<string>> header in responseMessage.Headers)
            {
                context.HttpContext.Response.Headers[header.Key] = new StringValues([.. header.Value]);
            }

            using Stream stream = await responseMessage.Content.ReadAsStreamAsync();
            await stream.CopyToAsync(context.HttpContext.Response.Body);
            await context.HttpContext.Response.Body.FlushAsync();
        }
    }
}
