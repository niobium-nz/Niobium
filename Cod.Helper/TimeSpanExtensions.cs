using System;

namespace Cod
{
    public static class TimeSpanExtensions
    {
        public static string ToDisplay(this TimeSpan timeSpan, bool displaySeconds = false)
        {
            var result = string.Empty;
            if (timeSpan.Days > 0)
            {
                result += $"{timeSpan.Days}天";
            }

            if (timeSpan.Hours > 0 || timeSpan.Minutes > 0)
            {
                result += $"{timeSpan.Hours}小时";
            }

            if (displaySeconds)
            {
                if (timeSpan.Minutes > 0 || timeSpan.Seconds > 0)
                {
                    result += $"{timeSpan.Minutes}分钟";
                }

                if (timeSpan.Seconds > 0)
                {
                    result += $"{timeSpan.Seconds}秒钟";
                }
            }
            else
            {
                if (timeSpan.Minutes > 0)
                {
                    result += $"{timeSpan.Minutes}分钟";
                }
            }

            return result;
        }
    }
}
