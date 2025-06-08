using System.Globalization;

namespace Cod
{
    public static class CultureInfoExtensions
    {
        public static bool TryParseCultureInfo(string cultureCode, out CultureInfo culture)
        {
            culture = null;

            try
            {
                CultureInfo[] cultures = CultureInfo.GetCultures(CultureTypes.AllCultures & ~CultureTypes.NeutralCultures);
                var c = cultures.FirstOrDefault(c => c.Name.Equals(cultureCode, StringComparison.OrdinalIgnoreCase));
                if (c != null)
                {
                    culture = c;
                    return true;
                }
            }
            catch (CultureNotFoundException)
            {
            }

            return false;
        }

        public static string ToTitleCase(this CultureInfo cultureInfo, string input)
        {
            _ = cultureInfo ?? throw new ArgumentNullException(nameof(cultureInfo));
            return string.IsNullOrWhiteSpace(input)
                ? throw new ArgumentNullException(nameof(input))
                : cultureInfo.TextInfo.ToTitleCase(input.Trim().ToLowerInvariant());
        }
    }
}
