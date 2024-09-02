namespace Cod
{
    public static class GuidExtensions
    {
        public static string ToKey(this Guid guid)
        {
            return CodSettings.UseLegacyGuidFormat ? guid.ToString("N").ToUpperInvariant() : guid.ToString();
        }
    }
}
