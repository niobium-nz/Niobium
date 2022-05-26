namespace Cod.Platform
{
    public interface IEntitlementStore
    {
        IReadOnlyDictionary<string, string> GetEntitlements(string role, string scope);
    }
}
