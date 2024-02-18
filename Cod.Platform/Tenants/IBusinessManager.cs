namespace Cod.Platform.Tenants
{
    public interface IBusinessManager
    {
        Task<Business> GetAsync(Guid id);
    }
}
