using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Cod.Platform
{
    public interface IOpenIDManager
    {
        Task RegisterAsync(IEnumerable<OpenIDRegistration> registrations);

        Task<IEnumerable<Model.OpenID>> GetChannelsAsync(Guid user);

        Task<IEnumerable<Model.OpenID>> GetChannelsAsync(Guid user, int kind);

        Task<Model.OpenID> GetChannelAsync(Guid user, int kind, string identifier);
    }
}
