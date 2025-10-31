namespace Niobium
{
    public static class DoubleExtension
    {
        public static double ChineseRound(this double n, int decimals = 2)
        {
            return Math.Round(n, decimals, MidpointRounding.AwayFromZero);
        }

        public static decimal ChineseRound(this decimal n, int decimals = 2)
        {
            return Math.Round(n, decimals, MidpointRounding.AwayFromZero);
        }
    }
}
