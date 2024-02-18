using Newtonsoft.Json;

namespace Cod.Platform.Messaging
{
    public sealed class IoTCommandResult
    {
        [JsonProperty("s")]
        public int Status { get; set; }

        [JsonProperty("j")]
        public string PayloadJSON { get; set; }

        [JsonProperty("t")]
        public DateTimeOffset ExecutedAt { get; set; }
    }
}
