using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Cod.Platform
{
    public class SSLCertificateApplication
    {
        public IEnumerable<DNSRecord> Requirements { get; set; }

        public Func<Task<SSLCertificateApplicationResult>> Result { get; set; }
    }
}
