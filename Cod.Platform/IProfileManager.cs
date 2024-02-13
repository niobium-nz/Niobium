using Azure.Data.Tables;
using Cod.Model;

namespace Cod.Platform
{
    public interface IProfileManager
    {
        Task<IEnumerable<T>> CreateAsync<T>(IEnumerable<T> entities, bool replaceIfExist)
            where T : Profile, ITableEntity;

        Task<IEnumerable<T>> UpdateAsync<T>(IEnumerable<T> entities)
            where T : Profile, ITableEntity;

        Task<IEnumerable<T>> CreateOrUpdateAsync<T>(IEnumerable<T> entities)
            where T : Profile, ITableEntity;

        Task<IEnumerable<T>> DeleteAsync<T>(IEnumerable<T> entities, bool successIfNotExist = false)
            where T : Profile, ITableEntity;

        Task<T> GetAsync<T>(Guid business, Guid user, bool fallbackToDefaultBusiness = true)
            where T : Profile, ITableEntity;
    }
}
