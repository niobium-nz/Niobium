using System;

namespace Cod
{
    public static class TimeSpanExtensions
    {
        public static string ToDisplay(this TimeSpan timeSpan)
        {
            var result = string.Empty;
            if (timeSpan.Days >= 1)
            {
                result += $"{timeSpan.Days}天";
            }

            if (timeSpan.Hours > 0 || timeSpan.Minutes > 0)
            {
                result += $"{timeSpan.Hours}小时";
            }

            if (timeSpan.Minutes > 0)
            {
                result += $"{timeSpan.Hours}分";
            }

            return result;
        }
    }
}
