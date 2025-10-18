using System.Text.Json.Serialization;

namespace Niobium.Finance
{
    [JsonConverter(typeof(AmountJsonConverter))]
    public struct Amount : IEquatable<Amount>
    {
        public static readonly Amount Zero = new();

        public Amount()
        {
        }

        public Amount(long cents)
        {
            Cents = cents;
        }

        public Amount(long cents, Currency currency) : this(cents)
        {
            Currency = currency;
        }

        public Amount(long cents, string currency) : this(cents, Currency.Parse(currency))
        {
        }

        public Amount(double dollars)
        {
            Cents = (long)Math.Round(dollars * 100, 0);
        }

        public Amount(double dollars, Currency currency) : this(dollars)
        {
            Currency = currency;
        }

        public Amount(double dollars, string currency) : this(dollars, Currency.Parse(currency))
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

        public static implicit operator long(Amount input)
        {
            return input.Cents;
        }

        public static implicit operator double(Amount input)
        {
            return Math.Round(input.Cents / 100d, 2);
        }

        public static Amount Parse(long cents) => new(cents);

        public static Amount Parse(long cents, Currency currency) => new(cents, currency);

        public static Amount Parse(long cents, string currency) => new(cents, currency);

        public static Amount Parse(double dollars) => new(dollars);

        public static Amount Parse(double dollars, Currency currency) => new(dollars, currency);

        public static Amount Parse(double dollars, string currency) => new(dollars, currency);
    }
}
