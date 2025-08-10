namespace Cod
{
    public static class GuidExtensions
    {
        public static string ToKey(this Guid key)
        {
            return CodSettings.UseLegacyGuidFormat ? key.ToString("N").ToUpperInvariant() : key.ToString();
        }
    }
}
