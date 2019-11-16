using System.Threading.Tasks;

namespace Cod.Platform
{
    public interface IAccountable
    {
        Task<string> GetAccountingPrincipalAsync();
    }
}
