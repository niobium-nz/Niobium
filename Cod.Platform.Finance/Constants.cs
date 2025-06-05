namespace Cod.Platform.Finance
{
    public abstract class Constants : Cod.Platform.Constants
    {
        public static readonly Currency CNY = Currency.Parse("CNY");
        public static readonly Currency USD = Currency.Parse("USD");
        public static readonly Currency AUD = Currency.Parse("AUD");
        public static readonly Currency NZD = Currency.Parse("NZD");
    }
}
