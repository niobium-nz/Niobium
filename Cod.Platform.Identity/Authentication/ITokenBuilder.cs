namespace Cod.Platform.Identity.Authentication
{
    public interface ITokenBuilder
    {
        Task<string> BuildAsync(
            string mainIdentity,
            IEnumerable<string> roles = null,
            IEnumerable<KeyValuePair<string, string>> entitlements = null,
            ushort validHours = 8,
            string audience = "cod.client");
    }
}
