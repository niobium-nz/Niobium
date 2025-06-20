using System.Text.Json.Serialization;

namespace Cod.Finance
{
    [JsonConverter(typeof(AmountJsonConverter))]
    public struct Amount : IEquatable<Amount>
    {
        public static readonly Amount Zero = new();

        public Amount()
        {
        }

        public long Cents { get; set; } = 0;

        public Currency Currency { get; set; } = Currency.USD;

        public override bool Equals(object? obj)
        {
            return obj is Amount amount && Equals(amount);
        }

        public bool Equals(Amount other)
        {
            return Cents == other.Cents &&
                   Currency.Equals(other.Currency);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Cents, Currency);
        }

        public override string ToString()
        {
            return Currency.ToDisplayLocal(Cents);
        }

        public static bool operator ==(Amount left, Amount right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Amount left, Amount right)
        {
            return !(left == right);
        }
    }
}
