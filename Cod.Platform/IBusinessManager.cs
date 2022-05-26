namespace Cod.Platform
{
    public interface IBusinessManager
    {
        Task<Business> GetAsync(Guid id);
    }
}
