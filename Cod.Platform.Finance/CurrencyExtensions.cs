namespace Cod.Platform.Finance
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

        public static string ToDisplayLocal(this Currency currency, long cents)
        {
            return ToDisplayLocal(currency, Math.Round(cents / 100m, 2));
        }

        public static string ToDisplayLocal(this Currency currency, decimal payment)
        {
            System.Globalization.CultureInfo culture = Currency.GetCulture(currency.Code);
            return $"{currency.Code}{payment.ToString("C2", culture)}";
        }
    }
}
