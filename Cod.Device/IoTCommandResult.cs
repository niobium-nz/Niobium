using Newtonsoft.Json;

namespace Cod.Device
{
    public sealed class IoTCommandResult
    {
        [JsonProperty("s")]
        public int Status { get; set; }

        [JsonProperty("j")]
        public required string PayloadJSON { get; set; }

        [JsonProperty("t")]
        public DateTimeOffset ExecutedAt { get; set; }
    }
}
