using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Cod.Platform
{
    public static class AliyunSDKCLient
    {
        private static readonly IEnumerable<KeyValuePair<string, string>> defaultParam = new[]
        {
            new KeyValuePair<string, string>("Format", "JSON"),
            new KeyValuePair<string, string>("Timestamp", DateTimeOffset.UtcNow.ToString("o").Replace("+00:00", "Z")),
            new KeyValuePair<string, string>("SignatureMethod", "HMAC-SHA1"),
            new KeyValuePair<string, string>("SignatureVersion", "1.0"),
            new KeyValuePair<string, string>("RegionId", "cn-hangzhou"),
        };

        public static async Task<HttpResponseMessage> MakeRequestAsync(Uri host, IEnumerable<KeyValuePair<string, string>> param, string apiKey, string apiSecret)
        {
            var dic = new List<KeyValuePair<string, string>>(defaultParam);
            dic.AddRange(param);
            dic.Add(new KeyValuePair<string, string>("SignatureNonce", Guid.NewGuid().ToString()));
            dic.Add(new KeyValuePair<string, string>("AccessKeyId", apiKey));
            var signature = IssueSignature(dic, apiSecret);
            dic.Add(new KeyValuePair<string, string>("Signature", signature));

            var builder = new UriBuilder(host);
            var query = HttpUtility.ParseQueryString(builder.Query);
            foreach (var item in dic)
            {
                query.Add(item.Key, item.Value);
            }
            builder.Query = query.ToString();
            var url = builder.ToString();

            using var httpclient = new HttpClient(HttpHandler.GetHandler(), false);
            return await httpclient.GetAsync(url);
        }

        private static string IssueSignature(IEnumerable<KeyValuePair<string, string>> param, string secret)
        {
            var stringToSign = ComposeStringToSign(HttpMethod.Get, param);
            return SHA.SHA1Base64(stringToSign, secret);
        }

        private static string ComposeStringToSign(HttpMethod method, IEnumerable<KeyValuePair<string, string>> param)
        {
            var sortedDictionary = new SortedDictionary<string, string>(
                param.ToDictionary(kv => kv.Key, kv => kv.Value), StringComparer.Ordinal);

            var canonicalizedQueryString = new StringBuilder();
            foreach (var p in sortedDictionary)
            {
                canonicalizedQueryString.Append("&")
                    .Append(AliyunStupidEncode(p.Key)).Append("=")
                    .Append(AliyunStupidEncode(p.Value));
            }

            var stringToSign = new StringBuilder();
            stringToSign.Append(method.ToString().ToUpperInvariant());
            stringToSign.Append("&");
            stringToSign.Append(AliyunStupidEncode("/"));
            stringToSign.Append("&");
            stringToSign.Append(AliyunStupidEncode(
                canonicalizedQueryString.ToString().Substring(1)));

            return stringToSign.ToString();
        }

        private static string AliyunStupidEncode(string value)
        {
            if (value is null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            var builder = new StringBuilder();
            foreach (var c in value)
            {
                var edcoded = HttpUtility.UrlEncode(c.ToString(), Encoding.UTF8);
                if (edcoded.Length > 1)
                {
                    builder.Append(edcoded.ToUpper(CultureInfo.InvariantCulture));
                }
                else
                {
                    builder.Append(c);
                }
            }

            return builder.ToString()
                .Replace("+", "%20")
                .Replace("*", "%2A")
                .Replace("%7E", "~");
        }
    }
}
