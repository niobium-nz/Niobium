using System;

namespace Cod.Platform
{
    internal class AliyunDNSQueryResponse
    {
        public int TotalCount { get; set; }

        public Guid RequestID { get; set; }

        public int PageSize { get; set; }

        public int PageNumber { get; set; }

        public AliyunDomainRecord DomainRecords { get; set; }
    }
}
