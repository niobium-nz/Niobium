
namespace Niobium.Finance
{
    public struct TaxableAmount : IEquatable<TaxableAmount>
    {
        public static readonly TaxableAmount Zero = new();

        public TaxableAmount()
        {
        }

        public TaxableAmount(Amount amount)
        {
            Amount = amount;
        }

        public TaxableAmount(Tax tax, Amount amount)
            : this(amount)
        {
            Tax = tax;
        }

        public Tax Tax { get; set; } = Tax.None;

        public Amount Amount { get; set; } = Amount.Zero;

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

        public static TaxableAmount Parse(Tax tax, Amount amount) => new(tax, amount);

        public static TaxableAmount Parse(Amount amount) => new(amount);
    }
}
