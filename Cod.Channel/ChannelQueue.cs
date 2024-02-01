using Cod.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Cod.Channel
{
    internal class ChannelQueue : IQueue
    {
        private const string MessageTemplatePlaceholder = "$$$MESSAGE$$$";
        private const string MessageTemplate = "<QueueMessage><MessageText>" + MessageTemplatePlaceholder + "</MessageText></QueueMessage>";
        private const string XMLMediaType = "application/xml";
        private static readonly Regex MessageRegex = new("<MessageText>(.*)</MessageText>");
        private static readonly IDictionary<string, string> StorageRequestHeaders = new Dictionary<string, string>
        {
            { "x-ms-version", "2018-03-28" },
        };
        private readonly IAuthenticator authenticator;
        private readonly IConfigurationProvider configuration;
        private readonly IHttpClient httpClient;

        public ChannelQueue(IAuthenticator authenticator, IConfigurationProvider configuration, IHttpClient httpClient)
        {
            this.authenticator = authenticator;
            this.configuration = configuration;
            this.httpClient = httpClient;
        }

        public Task<DisposableQueueMessage> DequeueAsync(string queueName, bool createIfNotExist = true, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<DisposableQueueMessage>> DequeueAsync(string queueName, int limit, bool createIfNotExist = true, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public async Task EnqueueAsync(IEnumerable<QueueMessage> entities, bool createIfNotExist = true, CancellationToken cancellationToken = default)
        {
            string endpoint = await configuration.GetSettingAsStringAsync(Constants.KEY_QUEUE_URL);
            IEnumerable<IGrouping<string, QueueMessage>> groups = entities.GroupBy(q => q.PartitionKey);
            foreach (IGrouping<string, QueueMessage> group in groups)
            {
                string queueName = group.Key;
                OperationResult<StorageSignature> sig = await authenticator.AquireSignatureAsync(StorageType.Queue, queueName, null, null);
                if (!sig.IsSuccess)
                {
                    if (sig.Code == InternalError.AuthenticationRequired)
                    {
                        throw new UnauthorizedAccessException();
                    }
                    else
                    {
                        throw new ApplicationException();
                    }
                }

                foreach (QueueMessage message in group)
                {
                    int timeout = 0;
                    if (message.Delay.HasValue)
                    {
                        timeout = (int)message.Delay.Value.TotalSeconds;
                    }
                    string url = $"{endpoint}/{queueName}/messages{sig.Result.Signature}&messagettl=-1&visibilitytimeout={timeout}";
                    string msg = message.Body is string str ? str : JsonSerializer.SerializeObject(message.Body);
                    OperationResult<string> result = await SendRequest(url, HttpMethod.Post, msg);
                    if (!result.IsSuccess)
                    {
                        if (result.Code == InternalError.AuthenticationRequired)
                        {
                            throw new UnauthorizedAccessException();
                        }
                        else
                        {
                            throw new ApplicationException();
                        }
                    }
                }
            }
        }

        public async Task<IEnumerable<QueueMessage>> PeekAsync(string queueName, int? limit, bool createIfNotExist = true, CancellationToken cancellationToken = default)
        {
            OperationResult<StorageSignature> sig = await authenticator.AquireSignatureAsync(StorageType.Queue, queueName, null, null);
            if (!sig.IsSuccess)
            {
                return Enumerable.Empty<QueueMessage>();
            }
            string endpoint = await configuration.GetSettingAsStringAsync(Constants.KEY_QUEUE_URL);
            string url = $"{endpoint}/{queueName}/messages{sig.Result.Signature}&peekonly=true";
            OperationResult<string> resp = await SendRequest(url, HttpMethod.Get);
            if (!resp.IsSuccess)
            {
                return Enumerable.Empty<QueueMessage>();
            }

            List<QueueMessage> result = new();
            MatchCollection matches = MessageRegex.Matches(resp.Result);
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
            List<KeyValuePair<string, string>> headers = new();
            foreach (string key in StorageRequestHeaders.Keys)
            {
                headers.Add(new KeyValuePair<string, string>(key, StorageRequestHeaders[key]));
            }
            headers.Add(new KeyValuePair<string, string>("x-ms-date", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()));

            OperationResult<string> result = await httpClient.RequestAsync<string>(
                method,
                url,
                body: MessageTemplate.Replace(MessageTemplatePlaceholder, Base64.Encode(message)),
                headers: headers,
                contentType: XMLMediaType);

            return !result.IsSuccess ? new OperationResult<string>(result) : new OperationResult<string>(result.Result);
        }


    }
}