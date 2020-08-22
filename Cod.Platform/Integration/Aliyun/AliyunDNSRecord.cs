namespace Cod.Platform
{
    internal class AliyunDNSRecord
    {
        public string RR { get; set; }

        public string Line { get; set; }

        public string Status { get; set; }

        public bool Locked { get; set; }

        public DNSRecordType Type { get; set; }

        public string DomainName { get; set; }

        public string Value { get; set; }

        public string RecordID { get; set; }

        public int TTL { get; set; }
    }
}
