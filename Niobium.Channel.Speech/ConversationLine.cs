namespace Niobium.Channel.Speech
{
    public class ConversationLine
    {
        public required string Text { get; set; }

        public string? Translation { get; set; }

        public string? SourceLanguage { get; set; }

        public string? TargetLanguage { get; set; }

        public required DateTimeOffset CreatedAt { get; set; }
    }
}
