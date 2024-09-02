namespace Cod.Platform.Tenant
{
    public interface IBrandService
    {
        Task<BrandingInfo> GetAsync(string name);

        Task<BrandingInfo> GetAsync(OpenIDKind kind, string app);
    }
}
