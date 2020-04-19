using System.Collections.Generic;
using System.Threading.Tasks;

namespace Cod.Platform
{
    public interface IOpenIDManager
    {
        Task RegisterAsync(IEnumerable<OpenIDRegistration> registrations);

        Task<IEnumerable<Model.OpenID>> GetChannelsAsync(string account);

        Task<IEnumerable<Model.OpenID>> GetChannelsAsync(string account, int kind);

        Task<Model.OpenID> GetChannelAsync(string account, int kind, string identifier);
    }
}
