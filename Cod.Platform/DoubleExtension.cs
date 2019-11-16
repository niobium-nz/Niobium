using System;

namespace Cod.Platform
{
    public static class DoubleExtension
    {
        public static double ChineseRound(this double n) => Math.Round(n, 2, MidpointRounding.AwayFromZero);
    }
}
