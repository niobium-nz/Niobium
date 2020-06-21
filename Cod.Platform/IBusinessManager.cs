using System;
using System.Threading.Tasks;

namespace Cod.Platform
{
    public interface IBusinessManager
    {
        Task<Business> GetAsync(Guid id);
    }
}
