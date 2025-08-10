using Cod.Identity;

namespace Cod.Platform.Identity
{
    internal sealed class DatabaseEntitlementStore(IRepository<Entitlement> repository) : IEntitlementDescriptor
    {
        public bool IsHighOverhead => true;

        public bool CanDescribe(Guid tenant, Guid user, string role)
        {
            return true;
        }

        public async Task<IEnumerable<EntitlementDescription>> DescribeAsync(Guid tenant, Guid user, string role)
        {
            Entitlement? record = await repository.RetrieveAsync(Entitlement.BuildPartitionKey(tenant), Entitlement.BuildRowKey(role));
            return record?.GetEntitlements() ?? [];
        }
    }
}
