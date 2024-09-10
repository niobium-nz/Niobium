namespace Cod.Platform.Identity
{
    public interface IEntitlementDescriptor
    {
        bool IsHighOverhead { get; }

        bool CanDescribe(Guid tenant, Guid user, string role);

        Task<IEnumerable<EntitlementDescription>> DescribeAsync(Guid tenant, Guid user, string role);
    }
}
