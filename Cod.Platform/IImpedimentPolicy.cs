using System.Collections.Generic;
using System.Threading.Tasks;

namespace Cod.Platform
{
    public interface IImpedimentPolicy
    {
        Task<bool> SupportAsync(IImpedimentContext context);

        Task<bool> ImpedeAsync(IImpedimentContext context);

        Task<bool> UnimpedeAsync(IImpedimentContext context);

        Task<IEnumerable<Impediment>> GetImpedimentsAsync(IImpedimentContext context);
    }
}
