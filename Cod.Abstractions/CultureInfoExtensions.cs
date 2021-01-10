using System;
using System.Globalization;

namespace Cod
{
    public static class CultureInfoExtensions
    {
        public static string ToTitleCase(this CultureInfo cultureInfo, string input)
        {
            _ = cultureInfo ?? throw new ArgumentNullException(nameof(cultureInfo));
            if (String.IsNullOrWhiteSpace(input))
            {
                throw new ArgumentNullException(nameof(input));
            }
            return cultureInfo.TextInfo.ToTitleCase(input.Trim().ToLowerInvariant());
        }
    }
}
