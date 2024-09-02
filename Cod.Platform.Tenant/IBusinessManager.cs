namespace Cod.Platform.Tenant
{
    public interface IBusinessManager
    {
        Task<Business> GetAsync(Guid id);
    }
}
