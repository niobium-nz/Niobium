namespace Niobium.Platform.Identity
{
    public interface ITokenBuilder
    {
        Task<string> BuildAsync(Guid tenant, string mainIdentity, IEnumerable<KeyValuePair<string, string>>? claims = null, string? audience = null, IEnumerable<string>? roles = null);
    }
}
