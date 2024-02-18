using System;

namespace Cod
{
    public static class DoubleExtension
    {
        public static double ChineseRound(this double n)
        {
            return Math.Round(n, 2, MidpointRounding.AwayFromZero);
        }
    }
}
