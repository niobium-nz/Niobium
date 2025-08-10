namespace Cod
{
    public class Permission
    {
        public required string Category { get; set; }

        public required string Scope { get; set; }

        public bool IsWildcard { get; set; }

        public IEnumerable<string> Entitlements { get; set; } = [];
    }
}
