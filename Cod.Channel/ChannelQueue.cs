using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Cod.Channel
{
    internal class ChannelQueue : IQueue
    {
        private const string MessageTemplatePlaceholder = "$$$MESSAGE$$$";
        private const string MessageTemplate = "<QueueMessage><MessageText>" + MessageTemplatePlaceholder + "</MessageText></QueueMessage>";
        private const string XMLMediaType = "application/xml";
        private static readonly Regex MessageRegex = new Regex("<MessageText>(.*)</MessageText>");
        private static readonly IDictionary<string, string> StorageRequestHeaders = new Dictionary<string, string>
        {
            { "x-ms-version", "2018-03-28" },
        };
        private readonly IAuthenticator authenticator;
        private readonly IConfigurationProvider configuration;
        private readonly HttpClient httpClient;

        public ChannelQueue(IAuthenticator authenticator, IConfigurationProvider configuration, HttpClient httpClient)
        {
            this.authenticator = authenticator;
            this.configuration = configuration;
            this.httpClient = httpClient;
        }

        public async Task<OperationResult> EnqueueAsync(IEnumerable<QueueMessage> entities)
        {
            var endpoint = await this.configuration.GetSettingAsStringAsync(Constants.KEY_QUEUE_URL);
            var groups = entities.GroupBy(q => q.PartitionKey);
            foreach (var group in groups)
            {
                var queueName = group.Key;
                var sig = await this.authenticator.AquireSignatureAsync(StorageType.Queue, queueName, null, null);
                if (!sig.IsSuccess)
                {
                    return sig;
                }

                foreach (var message in group)
                {
                    var timeout = 0;
                    if (message.Delay.HasValue)
                    {
                        timeout = (int)message.Delay.Value.TotalSeconds;
                    }
                    var url = $"{endpoint}/{queueName}/messages{sig.Result.Signature}&messagettl=-1&visibilitytimeout={timeout}";
                    var msg = message.Body is string str ? str : JsonConvert.SerializeObject(message.Body);
                    var result = await SendRequest(url, HttpMethod.Post, msg);
                    if (!result.IsSuccess)
                    {
                        return result;
                    }
                }
            }

            return OperationResult.Create();
        }

        public async Task<IEnumerable<QueueMessage>> PeekAsync(string queueName, int limit)
        {
            var sig = await this.authenticator.AquireSignatureAsync(StorageType.Queue, queueName, null, null);
            if (!sig.IsSuccess)
            {
                return Enumerable.Empty<QueueMessage>();
            }
            var endpoint = await this.configuration.GetSettingAsStringAsync(Constants.KEY_QUEUE_URL);
            var url = $"{endpoint}/{queueName}/messages{sig.Result.Signature}&peekonly=true";
            var resp = await this.SendRequest(url, HttpMethod.Get);
            if (!resp.IsSuccess)
            {
                return Enumerable.Empty<QueueMessage>();
            }

            var result = new List<QueueMessage>();
            var matches = MessageRegex.Matches(resp.Result);
            foreach (Match match in matches)
            {
                if (match.Success)
                {
                    result.Add(new QueueMessage
                    {
                        Body = match.Value,
                        PartitionKey = queueName
                    });
                }
            }

            return result;
        }

        protected virtual async Task<OperationResult<string>> SendRequest(string url, HttpMethod method, string message = null)
        {
            var headers = new List<KeyValuePair<string, string>>();
            foreach (var key in StorageRequestHeaders.Keys)
            {
                headers.Add(new KeyValuePair<string, string>(key, StorageRequestHeaders[key]));
            }
            headers.Add(new KeyValuePair<string, string>("x-ms-date", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()));

            var result = await httpClient.RequestAsync<string>(
                method,
                url,
                body: MessageTemplate.Replace(MessageTemplatePlaceholder, Base64.Encode(message)),
                headers: headers,
                contentType: XMLMediaType);

            if (!result.IsSuccess)
            {
                return new OperationResult<string>(result);
            }

            return OperationResult<string>.Create(result.Result);
        }
    }
}