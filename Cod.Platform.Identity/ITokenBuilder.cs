namespace Cod.Platform.Identity
{
    public interface ITokenBuilder
    {
        Task<string> BuildAsync(
            string mainIdentity,
            IEnumerable<KeyValuePair<string, string>> entitlements = null,
            ushort validHours = 8,
            string audience = "cod.client");
    }
}
