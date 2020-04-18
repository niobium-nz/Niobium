using System.Collections.Generic;
using System.Threading.Tasks;

namespace Cod.Platform
{
    public interface IOpenIDManager
    {
        Task RegisterAsync(string account, int kind, string identity, bool overrideIfExists);

        Task<IEnumerable<OpenID>> GetChannelsAsync(string account);

        Task<IEnumerable<OpenID>> GetChannelsAsync(string account, int kind);

        Task<OpenID> GetChannelAsync(string account, int kind, string identifier);
    }
}
