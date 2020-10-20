using System.Threading.Tasks;

namespace Cod.Platform
{
    public interface IBrandService
    {
        Task<BrandingInfo> GetAsync(string name);

        Task<BrandingInfo> GetAsync(OpenIDKind kind, string app);
    }
}
