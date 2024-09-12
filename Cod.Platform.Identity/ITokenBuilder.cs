namespace Cod.Platform.Identity
{
    public interface ITokenBuilder
    {
        Task<string> BuildAsync(string mainIdentity, IEnumerable<KeyValuePair<string, string>>? claims = null, string? audience = null);
    }
}
