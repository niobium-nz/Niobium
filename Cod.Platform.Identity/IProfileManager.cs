namespace Cod.Platform.Identity
{
    public interface IProfileManager
    {
        Task<IEnumerable<T>> CreateAsync<T>(IEnumerable<T> entities, bool replaceIfExist)
            where T : Profile;

        Task<IEnumerable<T>> UpdateAsync<T>(IEnumerable<T> entities)
            where T : Profile;

        Task<IEnumerable<T>> CreateOrUpdateAsync<T>(IEnumerable<T> entities)
            where T : Profile;

        Task DeleteAsync<T>(IEnumerable<T> entities, bool successIfNotExist = false)
            where T : Profile;

        Task<T> GetAsync<T>(Guid business, Guid user, bool fallbackToDefaultBusiness = true)
            where T : Profile;
    }
}
