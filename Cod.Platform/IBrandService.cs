using System.Threading.Tasks;
using Cod.Platform.Model;

namespace Cod.Platform
{
    public interface IBrandService
    {
        Task<BrandingInfo> GetAsync(string name);

        Task<BrandingInfo> GetAsync(OpenIDKind kind, string app);
    }
}
