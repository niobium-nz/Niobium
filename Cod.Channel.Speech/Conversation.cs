namespace Cod.Channel.Speech
{
    public class Conversation
    {
        public required string ID { get; set; }

        public required List<ConversationLine> Lines { get; set; }

        public required DateTimeOffset CreatedAt { get; set; }

        public string? ErrorMessage { get; set; }
    }
}
