using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Pathoschild.Http.Client;

namespace Cod.Platform
{
    public static class Phone
    {
        private const string DELIVERY_SMS_PARAM = "{\"waybill\":\"WAYBILL\",\"code\":\"CODE\",\"address\":\"ADDRESS\",\"name\":\"NAME\",\"tel\":\"TEL\"}";
        private const string REGISTRATION_SMS_PARAM = "{\"code\":\"CODE\"}";

        public static async Task<bool> SendAliyunRegistrationSmsAsync(string mobile, string source, string code, ILogger log)
            => await SendAliyunSmsAsync(mobile, source, "SMS_172980267", REGISTRATION_SMS_PARAM.Replace("CODE", code), log);

        public static async Task<bool> SendAliyunDeliverySmsAsync(string mobile, string source, string code, string waybill, string address, string name, string contact, ILogger log)
        {
            if (waybill.Length >= 20)
            {
                waybill = $"{waybill.Substring(0, 16)}...";
            }
            if (address.Length >= 20)
            {
                address = $"{address.Substring(0, 16)}...";
            }

            return await SendAliyunSmsAsync(mobile, source, "SMS_172882306", DELIVERY_SMS_PARAM.Replace("WAYBILL", waybill)
                        .Replace("CODE", $"{code.Substring(0, 4)}-{code.Substring(4, 4)}-{code.Substring(8, 4)}")
                        .Replace("ADDRESS", address)
                        .Replace("NAME", name)
                        .Replace("TEL", contact), log);
        }

        public static async Task SendTwilioSmsAsync(string to, string content, ILogger log)
        {
            var cfg = new ConfigurationProvider();
            var twilioAccount = await cfg.GetSettingAsync("TWILIO-ACCOUNT");
            var twilioToken = await cfg.GetSettingAsync("TWILIO-TOKEN");
            var twilioNumber = await cfg.GetSettingAsync("TWILIO-NUMBER");
            if (String.IsNullOrWhiteSpace(twilioAccount)
                || String.IsNullOrWhiteSpace(twilioToken)
                 || String.IsNullOrWhiteSpace(twilioNumber))
            {
                throw new ArgumentNullException("Twilio.Config");
            }

            using (var httpclient = new HttpClient(HttpHandler.GetHandler(), false))
            using (var client = new FluentClient("https://api.twilio.com/", httpclient).SetOptions(true, true))
            {
                var r = await client.PostAsync($"2010-04-01/Accounts/{twilioAccount}/Messages.json")
                    .WithBasicAuthentication(twilioAccount, twilioToken)
                    .WithBody(builder => new FormUrlEncodedContent(new List<KeyValuePair<string, string>>
                    {
                        new KeyValuePair<string, string>("To", to),
                        new KeyValuePair<string, string>("From", twilioNumber),
                        new KeyValuePair<string, string>("Body", content)
                    }));
                var code = (int)r.Status;
                if (code < 200 || code >= 400)
                {
                    var body = await r.AsString();
                    log.LogCritical($"Error occured while making request to twilio: {body}");
                }
            }
        }

        public static async Task MakeTwilioCallAsync(string to, string scriptURL, ILogger log)
        {
            var cfg = new ConfigurationProvider();
            var twilioAccount = await cfg.GetSettingAsync("TWILIO-ACCOUNT");
            var twilioToken = await cfg.GetSettingAsync("TWILIO-TOKEN");
            var twilioNumber = await cfg.GetSettingAsync("TWILIO-NUMBER");
            if (String.IsNullOrWhiteSpace(twilioAccount)
                || String.IsNullOrWhiteSpace(twilioToken)
                 || String.IsNullOrWhiteSpace(twilioNumber))
            {
                throw new ArgumentNullException("Twilio.Config");
            }

            using (var httpclient = new HttpClient(HttpHandler.GetHandler(), false))
            using (var client = new FluentClient("https://api.twilio.com/", httpclient).SetOptions(true, true))
            {
                var r = await client.PostAsync($"2010-04-01/Accounts/{twilioAccount}/Calls.json")
                    .WithBasicAuthentication(twilioAccount, twilioToken)
                    .WithBody(builder => new FormUrlEncodedContent(new List<KeyValuePair<string, string>>
                    {
                        new KeyValuePair<string, string>("To", to),
                        new KeyValuePair<string, string>("From", twilioNumber),
                        new KeyValuePair<string, string>("Url", scriptURL)
                    }));
                var code = (int)r.Status;
                if (code < 200 || code >= 400)
                {
                    var body = await r.AsString();
                    log.LogCritical($"Error occured while making request to twilio: {body}");
                }
            }
        }

        private static async Task<bool> SendAliyunSmsAsync(string mobile, string source, string templateID, string templateParams, ILogger log)
        {
            if (String.IsNullOrWhiteSpace(mobile)
                || String.IsNullOrWhiteSpace(templateID)
                || String.IsNullOrWhiteSpace(templateParams))
            {
                return false;
            }
            mobile = mobile.Trim();
            if (mobile.Length != 11 || !mobile.All(Char.IsDigit))
            {
                return false;
            }

            var cfg = new ConfigurationProvider();
            var key = await cfg.GetSettingAsync("SMS-ACCESS-KEY");
            var secret = await cfg.GetSettingAsync("SMS-ACCESS-SECRET");

            var dic = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("Version", "2017-05-25"),
                new KeyValuePair<string, string>("Action", "SendSms"),
                new KeyValuePair<string, string>("Format", "JSON"),
                new KeyValuePair<string, string>("PhoneNumbers", mobile),
                new KeyValuePair<string, string>("SignName", source),
                new KeyValuePair<string, string>("TemplateCode", templateID),
                new KeyValuePair<string, string>("TemplateParam", templateParams),
                new KeyValuePair<string, string>("Timestamp", DateTimeOffset.UtcNow.ToString("o").Replace("+00:00", "Z")),
                new KeyValuePair<string, string>("SignatureMethod", "HMAC-SHA1"),
                new KeyValuePair<string, string>("SignatureVersion", "1.0"),
                new KeyValuePair<string, string>("SignatureNonce", Guid.NewGuid().ToString()),
                new KeyValuePair<string, string>("AccessKeyId", key),
                new KeyValuePair<string, string>("RegionId", "cn-hangzhou"),
            };


            var stringToSign = ComposeStringToSign(HttpMethod.Get, dic);
            var signature = SHA1Base64(stringToSign, secret + "&");
            dic.Add(new KeyValuePair<string, string>("Signature", signature));

            using (var httpclient = new HttpClient(await HttpHandler.GetProxyHandler(), false))
            using (var client = new FluentClient("http://dysmsapi.niaoju.net/", httpclient).SetOptions(true, true))
            {
                var response = await client.GetAsync("/").WithArguments(dic).AsString();
                if (response.Contains("\"Message\":\"OK\""))
                {
                    log.LogInformation($"Success to send SMS via Aliyun with proxy");
                    return true;
                }
                else
                {
                    log.LogError($"Failed to send SMS via Aliyun with proxy and return error: {response}");
                    return false;
                }
            }
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
            var stringBuilder = new StringBuilder();
            var text = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_.~";
            var bytes = Encoding.UTF8.GetBytes(value);
            foreach (char c in bytes)
            {
                if (text.IndexOf(c) >= 0)
                {
                    stringBuilder.Append(c);
                }
                else
                {
                    stringBuilder.Append("%").Append(String.Format(CultureInfo.InvariantCulture, "{0:X2}", (int)c));
                }
            }

            return stringBuilder.ToString();
        }

        private static string SHA1Base64(string data, string key)
        {
            using (var hmac = new HMACSHA1(Encoding.UTF8.GetBytes(key)))
            {
                var hashValue = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
                return Convert.ToBase64String(hashValue);
            }
        }
    }
}
