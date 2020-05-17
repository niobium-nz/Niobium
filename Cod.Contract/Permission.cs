using System.Collections.Generic;

namespace Cod
{
    public class Permission
    {
        public string Category { get; set; }

        public string Scope { get; set; }

        public bool IsWildcard { get; set; }

        public IEnumerable<string> Entitlements { get; set; }
    }
}
