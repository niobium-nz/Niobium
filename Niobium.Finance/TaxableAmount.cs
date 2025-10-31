
namespace Niobium.Finance
{
    public struct TaxableAmount : IEquatable<TaxableAmount>
    {
        public static readonly TaxableAmount Zero = new();

        public TaxableAmount()
        {
        }

        public TaxableAmount(Amount amountIncludeTax)
        {
            Amount = amountIncludeTax;
        }

        public TaxableAmount(Tax tax, Amount amountIncludeTax)
            : this(amountIncludeTax)
        {
            Tax = tax;
        }

        /// <summary>
        /// The tax applied to the amount.
        /// </summary>
        public Tax Tax { get; set; } = Tax.None;

        /// <summary>
        /// The amount including tax.
        /// </summary>
        public Amount Amount { get; set; } = Amount.Zero;

        /// <summary>
        /// The amount before tax.
        /// </summary>
        public readonly Amount AmountBeforeTax => Amount.Parse(Tax.FigureCentsBeforeTax(Amount), Amount.Currency);

        public override bool Equals(object? obj)
        {
            return obj is TaxableAmount amount && Equals(amount);
        }

        public bool Equals(TaxableAmount other)
        {
            return Tax.Equals(other.Tax) &&
                   Amount.Equals(other.Amount);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Tax, Amount);
        }

        public static bool operator ==(TaxableAmount left, TaxableAmount right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(TaxableAmount left, TaxableAmount right)
        {
            return !(left == right);
        }

        public static TaxableAmount Parse(Tax tax, Amount amountIncludeTax) => new(tax, amountIncludeTax);

        public static TaxableAmount Parse(Amount amountIncludeTax) => new(amountIncludeTax);
    }
}
