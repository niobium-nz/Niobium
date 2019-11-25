using System.Collections.Generic;
using System.Threading.Tasks;
using Cod.Contract;

namespace Cod.Platform
{
    public interface IImpedimentPolicy
    {
        Task<bool> SupportAsync<T>(IImpedimentContext<T> context) where T : IImpedable;

        Task<bool> ImpedeAsync<T>(IImpedimentContext<T> context) where T : IImpedable;

        Task<bool> UnimpedeAsync<T>(IImpedimentContext<T> context) where T : IImpedable;

        Task<IEnumerable<Impediment>> GetImpedimentsAsync<T>(IImpedimentContext<T> context) where T : IImpedable;
    }
}
