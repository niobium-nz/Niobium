using System;
using System.Globalization;

namespace Cod
{
    public static class DateTimeOffsetExtensions
    {
        private const int WeeksInLongYear = 53;
        private const int WeeksInShortYear = 52;

        private const int MinWeek = 1;
        private const int MaxWeek = WeeksInLongYear;

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

        public static string ToISO8601(this DateTimeOffset input, CultureInfo culture)
        {
            _ = culture ?? throw new ArgumentNullException(nameof(culture));
            return input.ToString("o", culture);
        }

        public static string ToYearMonth(this DateTimeOffset input, CultureInfo culture)
        {
            _ = culture ?? throw new ArgumentNullException(nameof(culture));
            return input.ToString("y", culture);
        }

        public static string ToRFC1123(this DateTimeOffset input, CultureInfo culture)
        {
            _ = culture ?? throw new ArgumentNullException(nameof(culture));
            return input.ToString("r", culture);
        }

        public static string ToHourMinute(this DateTimeOffset input, CultureInfo culture)
        {
            _ = culture ?? throw new ArgumentNullException(nameof(culture));
            return input.ToString("t", culture);
        }

        public static string ToHourMinuteSecond(this DateTimeOffset input, CultureInfo culture)
        {
            _ = culture ?? throw new ArgumentNullException(nameof(culture));
            return input.ToString("T", culture);
        }

        public static string ToYearMonthDayHourMinuteSecond(this DateTimeOffset input, CultureInfo culture)
        {
            _ = culture ?? throw new ArgumentNullException(nameof(culture));
            return input.ToString("G", culture);
        }

        public static string ToYearMonthDayHourMinuteSecondInNames(this DateTimeOffset input, CultureInfo culture)
        {
            _ = culture ?? throw new ArgumentNullException(nameof(culture));
            return input.ToString("F", culture);
        }

        public static string ToYearMonthDayHourMinute(this DateTimeOffset input, CultureInfo culture)
        {
            _ = culture ?? throw new ArgumentNullException(nameof(culture));
            return input.ToString("g", culture);
        }

        public static string ToYearMonthDay(this DateTimeOffset input, CultureInfo culture)
        {
            _ = culture ?? throw new ArgumentNullException(nameof(culture));
            return input.ToString("d", culture);
        }

        public static string ToYearMonthDayInNames(this DateTimeOffset input, CultureInfo culture)
        {
            _ = culture ?? throw new ArgumentNullException(nameof(culture));
            return input.ToString("D", culture);
        }

        public static string ToMonthDay(this DateTimeOffset input, CultureInfo culture)
        {
            _ = culture ?? throw new ArgumentNullException(nameof(culture));
            return input.ToString("m", culture);
        }

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

        public static DateTimeOffset ToLocal(this DateTimeOffset dateTimeOffset, TimeZoneInfo timeZoneInfo)
        {
            _ = timeZoneInfo ?? throw new ArgumentNullException(nameof(timeZoneInfo));
            return TimeZoneInfo.ConvertTime(dateTimeOffset, timeZoneInfo);
        }

        public static string ToDisplayLocalShortDate(this DateTimeOffset dateTimeOffset, TimeZoneInfo timeZoneInfo) => dateTimeOffset.ToLocal(timeZoneInfo).DateTime.ToShortDateString();

        public static string ToDisplayLocalLongDate(this DateTimeOffset dateTimeOffset, TimeZoneInfo timeZoneInfo) => dateTimeOffset.ToLocal(timeZoneInfo).DateTime.ToLongDateString();

        public static string ToDisplayLocal(this DateTimeOffset dateTimeOffset, TimeZoneInfo timeZoneInfo)
        {
            _ = timeZoneInfo ?? throw new ArgumentNullException(nameof(timeZoneInfo));
            return dateTimeOffset.ToLocal(timeZoneInfo).DateTime.ToString();
        }

        public static int GetWeekOfYear(this DateTimeOffset offset) => GetWeekOfYear(offset.UtcDateTime);

        public static int GetWeekOfYear(this DateTime date)
        {
            int week = GetWeekNumber(date);

            if (week < MinWeek)
            {
                // If the week number obtained equals 0, it means that the
                // given date belongs to the preceding (week-based) year.
                return GetWeeksInYear(date.Year - 1);
            }

            if (week > GetWeeksInYear(date.Year))
            {
                // If a week number of 53 is obtained, one must check that
                // the date is not actually in week 1 of the following year.
                return MinWeek;
            }

            return week;
        }

        public static int GetWeeksInYear(int year)
        {
            if (year < 1000 || year >= 3000)
            {
                throw new ArgumentOutOfRangeException(nameof(year));
            }

            static int P(int y) => (y + (y / 4) - (y / 100) + (y / 400)) % 7;

            if (P(year) == 4 || P(year - 1) == 3)
            {
                return WeeksInLongYear;
            }

            return WeeksInShortYear;
        }

        public static DateTimeOffset GetFirstDateOfWeek(int year, int weekOfYear)
        {
            DateTime jan1 = new DateTime(year, 1, 1);
            int daysOffset = DayOfWeek.Thursday - jan1.DayOfWeek;

            // Use first Thursday in January to get first week of the year as
            // it will never be in Week 52/53
            DateTime firstThursday = jan1.AddDays(daysOffset);
            var cal = CultureInfo.CurrentCulture.Calendar;
            int firstWeek = cal.GetWeekOfYear(firstThursday, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);

            var weekNum = weekOfYear;
            // As we're adding days to a date in Week 1,
            // we need to subtract 1 in order to get the right date for week #1
            if (firstWeek == 1)
            {
                weekNum -= 1;
            }

            // Using the first Thursday as starting week ensures that we are starting in the right year
            // then we add number of weeks multiplied with days
            var result = firstThursday.AddDays(weekNum * 7);

            // Subtract 3 days from Thursday to get Monday, which is the first weekday in ISO8601
            return new DateTimeOffset(result.AddDays(-3), TimeSpan.Zero);
        }

        private static int GetWeekNumber(DateTime date)
        {
            return (date.DayOfYear - GetWeekday(date.DayOfWeek) + 10) / 7;
        }

        private static int GetWeekday(DayOfWeek dayOfWeek)
        {
            return dayOfWeek == DayOfWeek.Sunday ? 7 : (int)dayOfWeek;
        }
    }
}
