namespace Cod.Platform.Tenant
{
    public interface ITenantOwned
    {
        byte[] GetTenantAuthenticationIdentifier();
    }
}
