using System.Text.Json;
using System.Text.Json.Serialization;

namespace Cod.Finance
{
    public class AmountJsonConverter : JsonConverter<Amount>
    {
        public override Amount Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var json = reader.GetString() ?? throw new JsonException("Amount cannot be null or empty.");
            var prototype = JsonSerializer.Deserialize<AmountPrototype>(json, options) ?? throw new JsonException("Failed to deserialize Amount.");
            return new Amount
            {
                Cents = prototype.Cents,
                Currency = prototype.Currency
            };
        }

        public override void Write(Utf8JsonWriter writer, Amount value, JsonSerializerOptions options)
        {
            var prototype = new AmountPrototype
            {
                Cents = value.Cents,
                Currency = value.Currency
            };
            JsonSerializer.Serialize(writer, prototype, options);
        }

        class AmountPrototype
        {
            public long Cents { get; set; }

            public Currency Currency { get; set; } = Currency.USD;
        }
    }
}
