
namespace Niobium.Finance
{
    public struct Tax : IEquatable<Tax>
    {
        public static readonly Tax None = new();

        public Tax()
        {
        }

        public Tax(long rate, TaxKind kind) 
        {
            Rate = rate;
            Kind = kind;
        }

        public long Rate { get; set; } = 0;

        public TaxKind Kind { get; set; } = TaxKind.None;

        public override bool Equals(object? obj)
        {
            return obj is Tax tax && Equals(tax);
        }

        public bool Equals(Tax other)
        {
            return Rate == other.Rate &&
                   Kind == other.Kind;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Rate, Kind);
        }

        public static bool operator ==(Tax left, Tax right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Tax left, Tax right)
        {
            return !(left == right);
        }

        public static implicit operator long(Tax tax)
        {
            return tax.Rate;
        }

        public static implicit operator double(Tax tax)
        {
            return Math.Round(tax.Rate / 10000d, 2);
        }
    }
}
