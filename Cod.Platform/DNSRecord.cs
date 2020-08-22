namespace Cod.Platform
{
    public class DNSRecord
    {
        public string Domain { get; set; }

        public string Record { get; set; }

        public DNSRecordType Type { get; set; }

        public string Value { get; set; }

        public string Reference { get; set; }
    }
}
