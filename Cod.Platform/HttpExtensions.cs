using System.Net;
using System.Text;

namespace Cod.Platform
{
    public static class HttpExtensions
    {
        private const string JsonMediaType = "application/json";

        public static Task<HttpResponseMessage> MakeHttpResponseMessageAsync(this ValidationState validationState)
        {
            if (validationState is null)
            {
                throw new ArgumentNullException(nameof(validationState));
            }

            HttpResponseMessage result = new(HttpStatusCode.BadRequest);
            string json = JsonSerializer.SerializeObject(validationState.ToDictionary());
            result.Content = new StringContent(json, Encoding.UTF8, JsonMediaType);
            return Task.FromResult(result);
        }
    }
}
