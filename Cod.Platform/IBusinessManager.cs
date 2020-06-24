using System;
using System.Threading.Tasks;

namespace Cod.Platform
{
    public interface IBusinessManager
    {
        Task<Cod.Platform.Model.Business> GetAsync(Guid id);
    }
}
