namespace Cod.Platform.OpenAI
{
    public class OpenAIServiceOptions
    {
        public required Dictionary<int, string> SystemPrompts { get; set; } = [];

        public required string Endpoint { get; set; }

        public required string Secret { get; set; }

        public bool Validate()
           => SystemPrompts.Count > 0
            && !string.IsNullOrWhiteSpace(Endpoint)
            && !string.IsNullOrWhiteSpace(Secret);
    }
}
