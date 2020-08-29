using System;

namespace Cod
{
    public static class GuidExtensions
    {
        public static string ToKey(this Guid guid)
        {
            return guid.ToString("N").ToUpperInvariant();
        }
    }
}
