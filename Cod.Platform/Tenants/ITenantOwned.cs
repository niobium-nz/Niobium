namespace Cod.Platform.Tenants
{
    public interface ITenantOwned
    {
        byte[] GetTenantAuthenticationIdentifier();
    }
}
