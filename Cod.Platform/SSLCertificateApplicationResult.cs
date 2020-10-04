namespace Cod.Platform
{
    public class SSLCertificateApplicationResult
    {
        public byte[] PFX { get; set; }

        public string PEMCert { get; set; }

        public string PEMKey { get; set; }
    }
}
