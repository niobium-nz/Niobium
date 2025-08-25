namespace Niobium
{
    public static class DoubleExtension
    {
        public static double ChineseRound(this double n)
        {
            return Math.Round(n, 2, MidpointRounding.AwayFromZero);
        }

        public static decimal ChineseRound(this decimal n)
        {
            return Math.Round(n, 2, MidpointRounding.AwayFromZero);
        }
    }
}
