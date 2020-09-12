namespace Cod.Platform
{
    public interface ITenantOwned
    {
        byte[] GetTenantAuthenticationIdentifier();
    }
}
