namespace Cod
{
    public static class CurrencyExtensions
    {
        public static string ToDisplayLocal(this Currency currency, float payment)
        {
            return ToDisplayLocal(currency, (decimal)payment);
        }

        public static string ToDisplayLocal(this Currency currency, double payment)
        {
            return ToDisplayLocal(currency, (decimal)payment);
        }

        public static string ToDisplayLocal(this Currency currency, int payment)
        {
            return ToDisplayLocal(currency, (decimal)payment);
        }

        public static string ToDisplayLocal(this Currency currency, decimal payment)
        {
            System.Globalization.CultureInfo culture = Currency.GetCulture(currency.Code);
            return $"{currency.Code}{payment.ToString("C2", culture)}";
        }
    }
}
