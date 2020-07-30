using System;

namespace Cod
{
    public static class DateTimeOffsetExtensions
    {
        private static readonly TimeSpan chinaTimeOffset = TimeSpan.FromHours(8);
        private static readonly long reverseUnixTimestampAnchor = DateTimeOffset.Parse("2050-01-01T00:00:00Z").ToUnixTimeMilliseconds();

        public static DateTimeOffset MaxValueForTableStorage { get; private set; } = DateTimeOffset.Parse("2100-01-01T00:00:00Z");

        public static DateTimeOffset MinValueForTableStorage { get; private set; } = DateTimeOffset.Parse("2008-01-01T00:00:00Z");

        public static long ToReverseUnixTimeMilliseconds(this DateTimeOffset input)
            => reverseUnixTimestampAnchor - input.ToUnixTimeMilliseconds();

        public static string ToReverseUnixTimestamp(this DateTimeOffset input)
            => input.ToReverseUnixTimeMilliseconds().ToString().PadLeft(12, '0');

        public static DateTimeOffset FromReverseUnixTimeMilliseconds(long reverseUnixTimestamp)
            => DateTimeOffset.FromUnixTimeMilliseconds(reverseUnixTimestampAnchor - reverseUnixTimestamp);

        public static string ToSixDigitsDate(this DateTimeOffset input)
            => input.ToString("yyyyMMdd");

        public static string ToDisplayCST(this DateTimeOffset input)
            => input.ToOffset(chinaTimeOffset).ToString("yyyy-MM-dd HH:mm:ss");

        public static string ToDisplayCSTShort(this DateTimeOffset input)
            => input.ToOffset(chinaTimeOffset).ToString("MM-dd");

        public static DateTimeOffset? ParseDate(this string dateString)
        {
            if (string.IsNullOrWhiteSpace(dateString) || dateString.Length != 8)
            {
                return null;
            }

            if (!int.TryParse(dateString.Substring(0, 4), out var year))
            {
                return null;
            }

            if (!int.TryParse(dateString.Substring(4, 2), out var month))
            {
                return null;
            }

            if (!int.TryParse(dateString.Substring(6, 2), out var day))
            {
                return null;
            }

            return new DateTimeOffset(year, month, day, 0, 0, 0, TimeSpan.Zero);
        }
    }
}
