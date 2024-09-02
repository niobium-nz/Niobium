namespace Cod.Platform.Identity.Authorization
{
    public interface IEntitlementStore
    {
        IReadOnlyDictionary<string, string> GetEntitlements(string role, string scope);
    }
}
