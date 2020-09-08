using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Cod.Channel
{
    internal static class TableStorageHelper
    {
        private const string AND_OPERATOR = " and ";
        private static readonly IEnumerable<KeyValuePair<string, string>> TableStorageRequestHeaders = new List<KeyValuePair<string, string>>
        {
            new KeyValuePair<string, string>("x-ms-version", "2015-12-11"),
            new KeyValuePair<string, string>("DataServiceVersion", "3.0;NetFx"),
            new KeyValuePair<string, string>("MaxDataServiceVersion", "3.0;NetFx"),
            new KeyValuePair<string, string>("Accept", "application/json;odata=minimalmetadata"),
        };

        public static async Task<OperationResult<TableQueryResult<T>>> GetAsync<T>(HttpClient httpClient,
            string baseUrl, string connectionString,
            string partitionKeyStart, string partitionKeyEnd, string rowKeyStart, string rowKeyEnd,
            ContinuationToken continuationToken = null, int count = -1)
        {
            if (httpClient is null)
            {
                throw new ArgumentNullException(nameof(httpClient));
            }

            if (baseUrl is null)
            {
                throw new ArgumentNullException(nameof(baseUrl));
            }

            if (connectionString is null)
            {
                throw new ArgumentNullException(nameof(connectionString));
            }

            var filter = new StringBuilder();
            if (!String.IsNullOrWhiteSpace(partitionKeyStart) && !String.IsNullOrWhiteSpace(partitionKeyEnd)
                && partitionKeyStart == partitionKeyEnd)
            {
                filter.Append(AND_OPERATOR);
                filter.Append($"PartitionKey eq '{partitionKeyStart}'");
            }
            else
            {
                if (!String.IsNullOrWhiteSpace(partitionKeyStart))
                {
                    filter.Append(AND_OPERATOR);
                    filter.Append($"PartitionKey ge '{partitionKeyStart}'");
                }

                if (!String.IsNullOrWhiteSpace(partitionKeyEnd))
                {
                    filter.Append(AND_OPERATOR);
                    filter.Append($"PartitionKey le '{partitionKeyEnd}'");
                }
            }

            if (!String.IsNullOrWhiteSpace(rowKeyStart) && !String.IsNullOrWhiteSpace(rowKeyEnd)
                && rowKeyStart == rowKeyEnd)
            {
                filter.Append(AND_OPERATOR);
                filter.Append($"RowKey eq '{rowKeyStart}'");
            }
            else
            {
                if (!String.IsNullOrWhiteSpace(rowKeyStart))
                {
                    filter.Append(AND_OPERATOR);
                    filter.Append($"RowKey ge '{rowKeyStart}'");
                }

                if (!String.IsNullOrWhiteSpace(rowKeyEnd))
                {
                    filter.Append(AND_OPERATOR);
                    filter.Append($"RowKey le '{rowKeyEnd}'");
                }
            }

            string filterPath = null;
            string countPath = null;
            if (filter.Length > 0)
            {
                filter = filter.Remove(0, AND_OPERATOR.Length);
                filterPath = $"&$filter=({WebUtility.UrlEncode(filter.ToString())})";
            }

            if (count > 0)
            {
                countPath = $"&$top={count}";
            }

            var resource = typeof(T).Name;
            var url = new StringBuilder(baseUrl);
            url.Append("/");
            url.Append(resource);
            if (connectionString[0] != '?')
            {
                url.Append("?");
            }
            url.Append(connectionString);

            if (filterPath != null)
            {
                url.Append(filterPath);
            }

            if (countPath != null)
            {
                url.Append(countPath);
            }

            var result = new TableQueryResult<T>
            {
                Data = new List<T>(),
            };

            while (true)
            {
                var u = new StringBuilder(url.ToString());

                if (continuationToken != null)
                {
                    if (!String.IsNullOrWhiteSpace(continuationToken.NextPartitionKey))
                    {
                        u.Append("&NextPartitionKey=");
                        u.Append(continuationToken.NextPartitionKey);
                    }

                    if (!String.IsNullOrWhiteSpace(continuationToken.NextRowKey))
                    {
                        u.Append("&NextRowKey=");
                        u.Append(continuationToken.NextRowKey);
                    }
                }

                var response = await httpClient.RequestAsync(
                    HttpMethod.Get,
                    u.ToString(),
                    headers: TableStorageRequestHeaders);

                if (!response.IsSuccess)
                {
                    return new OperationResult<TableQueryResult<T>>(response);
                }

                string nextPK = null, nextRK = null;
                if (response.Result.Headers.TryGetValues("x-ms-continuation-NextPartitionKey", out var npk))
                {
                    nextPK = npk.Single();
                }
                if (response.Result.Headers.TryGetValues("x-ms-continuation-NextRowKey", out var nrk))
                {
                    nextRK = nrk.Single();
                }

                if (nextPK != null || nextRK != null)
                {
                    result.ContinuationToken = new ContinuationToken
                    {
                        NextPartitionKey = nextPK,
                        NextRowKey = nextRK,
                    };
                }

                var responseBody = await response.Result.Content.ReadAsStringAsync();

                // REMARK (5he11) 因 TABLE REST API 返回的 ETAG 的名称不是其类型上定义的命名，因此在反序列化前临时替换一下名字
                var tmpData = responseBody.Replace("\"odata.etag\":", "\"ETag\":");
                var objs = JsonConvert.DeserializeObject<TableStorageResult<T>>(tmpData);
                if (objs.Value.Count > 0)
                {
                    result.Data.AddRange(objs.Value);
                }

                if (!result.HasMore)
                {
                    break;
                }

                if (count > 0 && result.Data.Count >= count)
                {
                    break;
                }
            }

            return new OperationResult<TableQueryResult<T>>(result);
        }
    }
}
