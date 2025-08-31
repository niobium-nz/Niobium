namespace Niobium.Finance
{
    public static class TaxableAmountExtensions
    {
        public static bool ValidateConsistency(this IEnumerable<TaxableAmount> taxableAmounts)
        {
            if (taxableAmounts is null || !taxableAmounts.Any())
            {
                return true;
            }

            Currency currency = taxableAmounts.First().Amount.Currency;
            Tax tax = taxableAmounts.First().Tax;
            return taxableAmounts.All(ta => ta.Amount.Currency == currency && ta.Tax == tax);
        }
    }
}
