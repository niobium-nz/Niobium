using System;
using System.Linq;

namespace Cod
{
    public static class Int32Extensions
    {
        public static string ToAccessible(this TimeSpan value)
            => ToAccessible(value.TotalSeconds);

        public static string ToAccessible(this double value)
            => ToAccessible((int)value);

        public static string ToAccessible(this int value)
            => ToAccessible(value.ToString("X"));

        public static string ToAccessible(this string hex)
            => hex.ToUpperInvariant()
            .Replace('B', 'K')
            .Replace('C', 'N')
            .Replace('D', 'U')
            .Replace('E', 'W')
            .Replace('F', 'X');

        public static bool TestAccessible(string value)
            => value.ToUpperInvariant().All(c => Char.IsDigit(c) || c == 'D' || c == 'E' || c == 'B' || c == 'G' || c == 'K' || c == 'N' || c == 'U' || c == 'W' || c == 'X');
    }
}
