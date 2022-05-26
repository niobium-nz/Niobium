namespace Cod.Platform
{
    public interface ITokenBuilder
    {
        Task<string> BuildAsync(
            string mainIdentity,
            IEnumerable<string> roles = null,
            IEnumerable<KeyValuePair<string, string>> entitlements = null,
            ushort validHours = 8);
    }
}
