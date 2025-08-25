namespace Niobium
{
    public static class GuidExtensions
    {
        public static string ToKey(this Guid key)
        {
            return LegacySettings.UseLegacyGuidFormat ? key.ToString("N").ToUpperInvariant() : key.ToString();
        }
    }
}
