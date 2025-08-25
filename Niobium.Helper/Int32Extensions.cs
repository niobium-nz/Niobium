namespace Niobium
{
    public static class Int32Extensions
    {
        public static string ToAccessible(this TimeSpan value)
        {
            return value.TotalSeconds.ToAccessible();
        }

        public static string ToAccessible(this double value)
        {
            return ((int)value).ToAccessible();
        }

        public static string ToAccessible(this int value)
        {
            return value.ToString("X").ToAccessible();
        }

        public static string ToAccessible(this string hex)
        {
            return hex.ToUpperInvariant()
                    .Replace('B', 'K')
                    .Replace('C', 'N')
                    .Replace('D', 'U')
                    .Replace('E', 'W')
                    .Replace('F', 'X');
        }

        public static bool TestAccessible(string value)
        {
            return value.ToUpperInvariant().All(c => char.IsDigit(c) || c == 'D' || c == 'E' || c == 'B' || c == 'G' || c == 'K' || c == 'N' || c == 'U' || c == 'W' || c == 'X');
        }
    }
}
