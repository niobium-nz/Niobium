namespace Cod
{
    public static class CurrencyExtensions
    {
        public static string ToDisplayLocal(this float payment, Currency currency)
        {
            return ToDisplayLocal((decimal)payment, currency);
        }

        public static string ToDisplayLocal(this double payment, Currency currency)
        {
            return ToDisplayLocal((decimal)payment, currency);
        }

        public static string ToDisplayLocal(this int payment, Currency currency)
        {
            return ToDisplayLocal((decimal)payment, currency);
        }

        public static string ToDisplayLocal(this decimal payment, Currency currency)
        {
            System.Globalization.CultureInfo culture = Currency.GetCulture(currency.Code);
            return $"{payment.ToString("C2", culture)} {currency.Code}";
        }
    }
}
