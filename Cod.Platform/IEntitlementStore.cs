using System.Collections.Generic;

namespace Cod.Platform
{
    public interface IEntitlementStore
    {
        IReadOnlyDictionary<string, string> Get(string role);
    }
}
