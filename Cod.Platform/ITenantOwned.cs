using System;

namespace Cod.Platform
{
    public interface ITenantOwned
    {
        Guid GetTenant();
    }
}
