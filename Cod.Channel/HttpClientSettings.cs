using System;
using System.Net.Http.Headers;

namespace Cod.Channel
{
    public static class HttpClientSettings
    {
        public static Uri BaseAddress { get; set; }

        public static TimeSpan? Timeout { get; set; }

        public static HttpRequestHeaders DefaultRequestHeaders { get; set; }

        public static long? MaxResponseContentBufferSize { get; set; }
    }
}
