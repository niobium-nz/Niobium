using Cod.Identity;

namespace Cod.Platform.Identity
{
    internal class DatabaseEntitlementStore(IRepository<Entitlement> repository) : IEntitlementDescriptor
    {
        public bool IsHighOverhead { get => true; }

        public bool CanDescribe(Guid tenant, Guid user, string role) => true;

        public async Task<IEnumerable<EntitlementDescription>> DescribeAsync(Guid tenant, Guid user, string role)
        {
            var record = await repository.RetrieveAsync(Entitlement.BuildPartitionKey(tenant), Entitlement.BuildRowKey(role));
            return record?.GetEntitlements() ?? [];
        }
    }
}
