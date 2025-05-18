namespace Cod.Profile
{
    public static class IProfileExtensions
    {
        public static Guid GetTenant(this IProfile profile) => profile.PartitionKey;
        public static Guid GetUser(this IProfile profile) => profile.RowKey;
    }
}
