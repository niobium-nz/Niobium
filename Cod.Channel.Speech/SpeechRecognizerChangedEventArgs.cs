using System.Text.Json.Serialization;

namespace Cod.Channel.Speech
{
    public class SpeechRecognizerChangedEventArgs(SpeechRecognizerChangedType type) : IDomainEvent
    {
        [JsonIgnore]
        public string ID { get; set; } = string.Empty;

        [JsonIgnore]
        public DateTimeOffset Occurried { get; set; }

        [JsonIgnore]
        public DomainEventAudience Source { get; set; }

        [JsonIgnore]
        public DomainEventAudience Target { get; set; }

        public SpeechRecognizerChangedType Type { get; } = type;
    }
}
