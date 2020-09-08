using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Cod.Platform
{
    internal class AliyunDNSHelper : IDNSManager
    {
        private static readonly Uri AliyunDNSHost = new Uri("https://alidns.aliyuncs.com/");
        private readonly Lazy<IConfigurationProvider> configuration;
        private readonly ILogger logger;

        public AliyunDNSHelper(Lazy<IConfigurationProvider> configuration, ILogger logger)
        {
            this.configuration = configuration;
            this.logger = logger;
        }

        public async Task<OperationResult<IEnumerable<DNSRecord>>> QueryRecordsAsync(string domain)
        {
            if (String.IsNullOrWhiteSpace(domain))
            {
                throw new ArgumentNullException(nameof(domain));
            }

            domain = domain.Trim().ToLowerInvariant();

            var key = await this.configuration.Value.GetSettingAsync<string>("ALIYUN_KEY");
            var secret = await this.configuration.Value.GetSettingAsync<string>("ALIYUN_SECRET");
            var result = await AliyunSDKCLient.MakeRequestAsync(
                AliyunDNSHost,
                new[]
                {
                    new KeyValuePair<string, string>("Version", "2015-01-09"),
                    new KeyValuePair<string, string>("Action", "DescribeDomainRecords"),
                    new KeyValuePair<string, string>("DomainName", domain),
                    new KeyValuePair<string, string>("PageSize", "500"),
                },
                key,
                secret);


            var body = await result.Content.ReadAsStringAsync();
            if (result.IsSuccessStatusCode && body.Contains("\"PageSize\":500"))
            {
                var response = JsonConvert.DeserializeObject<AliyunDNSQueryResponse>(body);
                return new OperationResult<IEnumerable<DNSRecord>>(
                    response.DomainRecords.Record.Select(r => new DNSRecord
                    {
                        Domain = r.DomainName,
                        Record = r.RR,
                        Type = r.Type,
                        Value = r.Value,
                        Reference = r.RecordID,
                    }));
            }
            else
            {
                this.logger.LogError($"Failed to query DNS records for {domain} at Aliyun and return error: {body}");
                return new OperationResult<IEnumerable<DNSRecord>>(InternalError.InternalServerError);
            }
        }

        public async Task<OperationResult> CreateRecordAsync(string domain, string recordName, DNSRecordType type, string recordValue)
        {
            if (String.IsNullOrWhiteSpace(domain))
            {
                throw new ArgumentNullException(nameof(domain));
            }

            if (String.IsNullOrWhiteSpace(recordName))
            {
                throw new ArgumentNullException(nameof(recordName));
            }

            if (String.IsNullOrWhiteSpace(recordValue))
            {
                throw new ArgumentNullException(nameof(recordValue));
            }

            domain = domain.Trim().ToLowerInvariant();
            recordName = recordName.Trim();
            recordValue = recordValue.Trim();

            var key = await this.configuration.Value.GetSettingAsync<string>("ALIYUN_KEY");
            var secret = await this.configuration.Value.GetSettingAsync<string>("ALIYUN_SECRET");

            var response = await AliyunSDKCLient.MakeRequestAsync(
                AliyunDNSHost,
                new[]
                {
                    new KeyValuePair<string, string>("Version", "2015-01-09"),
                    new KeyValuePair<string, string>("Action", "AddDomainRecord"),
                    new KeyValuePair<string, string>("DomainName", domain),
                    new KeyValuePair<string, string>("RR", recordName),
                    new KeyValuePair<string, string>("Type", type.ToString()),
                    new KeyValuePair<string, string>("Value", recordValue),
                },
                key,
                secret);

            var body = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode && body.Contains("\"RecordId\":\""))
            {
                return OperationResult.Success;
            }
            else
            {
                this.logger.LogError($"Failed to create DNS record {recordName}.{domain}@{type} => {recordValue} at Aliyun and return error: {body}");
                return OperationResult.InternalServerError;
            }
        }

        public async Task<OperationResult> RemoveRecordAsync(string domain, string recordName, DNSRecordType type)
        {
            if (String.IsNullOrWhiteSpace(domain))
            {
                throw new ArgumentNullException(nameof(domain));
            }

            if (String.IsNullOrWhiteSpace(recordName))
            {
                throw new ArgumentNullException(nameof(recordName));
            }

            domain = domain.Trim().ToLowerInvariant();
            recordName = recordName.Trim();

            var key = await this.configuration.Value.GetSettingAsync<string>("ALIYUN_KEY");
            var secret = await this.configuration.Value.GetSettingAsync<string>("ALIYUN_SECRET");

            var query = await this.QueryRecordsAsync(domain);
            if (!query.IsSuccess)
            {
                return query;
            }

            var records = query.Result.Where(r => r.Record == recordName && r.Type == type);
            foreach (var record in records)
            {
                var response = await AliyunSDKCLient.MakeRequestAsync(
                AliyunDNSHost,
                new[]
                {
                    new KeyValuePair<string, string>("Version", "2015-01-09"),
                    new KeyValuePair<string, string>("Action", "DeleteDomainRecord"),
                    new KeyValuePair<string, string>("RecordId", record.Reference),
                },
                key,
                secret);

                var body = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode || !body.Contains("\"RecordId\":\""))
                {
                    this.logger.LogError($"Failed to remove DNS record {recordName}.{domain}@{type} at Aliyun and return error: {body}");
                    return OperationResult.InternalServerError;
                }
            }

            return OperationResult.Success;
        }

        public bool Support(string domain, DNSServiceProvider serviceProvider) => serviceProvider == DNSServiceProvider.Aliyun;
    }
}
