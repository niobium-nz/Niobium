using System.Collections.Generic;
using System.Threading.Tasks;

namespace Cod.Platform
{
    public interface IOpenIDManager
    {
        Task RegisterAsync(string account, int kind, string identity, bool overrideIfExists, string offsetPrefix = null);

        Task<IEnumerable<Model.OpenID>> GetChannelsAsync(string account);

        Task<IEnumerable<Model.OpenID>> GetChannelsAsync(string account, int kind);

        Task<Model.OpenID> GetChannelAsync(string account, int kind, string identifier);
    }
}
