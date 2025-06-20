using System.Text.Json;
using System.Text.Json.Serialization;

namespace Cod.Finance
{
    public class CurrencyJsonConverter : JsonConverter<Currency>
    {
        public override Currency Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return Currency.Parse(reader.GetString() ?? throw new JsonException("Currency code cannot be null or empty."));
        }

        public override void Write(Utf8JsonWriter writer, Currency value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.Code);
        }
    }
}
