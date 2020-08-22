using System.Threading.Tasks;

namespace Cod.Platform
{
    public interface ICertificateAuthority
    {
        Task<SSLCertificateApplication> ApplySSLCertificateAsync(
            string domain,
            string email,
            string password,
            string country,
            string state = null,
            string city = null,
            string organization = null,
            string unit = null);
    }
}
