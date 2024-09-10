namespace Cod.Platform.Identity
{
    public interface ITokenBuilder
    {
        Task<string> BuildAsync(
            string mainIdentity,
            IEnumerable<KeyValuePair<string, string>>? claims = null,
            string? symmetricSecurityKey = null,
            ushort validHours = 8,
            string audience = Constants.IDTokenDefaultAudience,
            string issuer = Constants.IDTokenDefaultIssuer);
    }
}
