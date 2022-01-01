using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Cod.Platform
{
    public interface IOpenIDManager
    {
        Task RegisterAsync(IEnumerable<OpenIDRegistration> registrations);

        Task<IEnumerable<OpenID>> GetChannelsAsync(Guid user);

        Task<IEnumerable<OpenID>> GetChannelsAsync(Guid user, int kind);
    }
}
