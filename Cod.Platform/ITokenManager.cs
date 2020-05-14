using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Cod.Platform
{
    public interface ITokenManager
    {
        Task<string> CreateAsync(Guid? group, Guid? user, string nameIdentifier, string contact, string openIDProvider, string openIDApp,
            IEnumerable<string> roles = null, IEnumerable<KeyValuePair<string, string>> entitlements = null);
    }
}
