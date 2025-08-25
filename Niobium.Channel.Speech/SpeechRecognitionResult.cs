namespace Niobium.Channel.Speech
{
    public class SpeechRecognitionResult
    {
        public required string ResultID { get; set; }

        public required long Duration { get; set; }

        public required long Offset { get; set; }

        public required SpeechRecognitionResultReason Reason { get; set; }

        public SpeechRecognitionNoMatchReason? NoMatchReason { get; set; }

        public string? ErrorDetails { get; set; }

        public string? Text { get; set; }

        public SpeechTranslationPair[]? Translations { get; set; }
    }
}
