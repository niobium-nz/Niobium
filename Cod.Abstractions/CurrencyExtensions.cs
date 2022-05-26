namespace Cod
{
    public static class CurrencyExtensions
    {
        public static string ToDisplayLocal(this Currency currency, float payment) => ToDisplayLocal(currency, (decimal)payment);

        public static string ToDisplayLocal(this Currency currency, double payment) => ToDisplayLocal(currency, (decimal)payment);

        public static string ToDisplayLocal(this Currency currency, int payment) => ToDisplayLocal(currency, (decimal)payment);

        public static string ToDisplayLocal(this Currency currency, decimal payment)
        {
            var culture = Currency.GetCulture(currency.Code);
            return $"{currency.Code}{payment.ToString("C2", culture)}";
        }
    }
}
