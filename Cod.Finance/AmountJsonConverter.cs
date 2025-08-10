using System.Text.Json;
using System.Text.Json.Serialization;

namespace Cod.Finance
{
    public sealed class AmountJsonConverter : JsonConverter<Amount>
    {
        public override Amount Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            string json = reader.GetString() ?? throw new JsonException("Amount cannot be null or empty.");
            AmountPrototype prototype = JsonSerializer.Deserialize<AmountPrototype>(json, options) ?? throw new JsonException("Failed to deserialize Amount.");
            return new Amount
            {
                Cents = prototype.Cents,
                Currency = prototype.Currency
            };
        }

        public override void Write(Utf8JsonWriter writer, Amount value, JsonSerializerOptions options)
        {
            AmountPrototype prototype = new()
            {
                Cents = value.Cents,
                Currency = value.Currency
            };
            JsonSerializer.Serialize(writer, prototype, options);
        }

        private sealed class AmountPrototype
        {
            public long Cents { get; set; }

            public Currency Currency { get; set; } = Currency.USD;
        }
    }
}
