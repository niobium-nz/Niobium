using System.Text.Json.Serialization;

namespace Niobium.Device
{
    public sealed class IoTCommandResult
    {
        [JsonPropertyName("s")]
        public int Status { get; set; }

        [JsonPropertyName("j")]
        public required string PayloadJSON { get; set; }

        [JsonPropertyName("t")]
        public DateTimeOffset ExecutedAt { get; set; }
    }
}
