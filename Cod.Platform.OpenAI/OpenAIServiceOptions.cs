namespace Cod.Platform.OpenAI
{
    public class OpenAIServiceOptions
    {
        public required string SystemPrompt { get; set; }

        public required string Endpoint { get; set; }

        public required string Secret { get; set; }

        public bool Validate()
           => !string.IsNullOrWhiteSpace(SystemPrompt)
            && !string.IsNullOrWhiteSpace(Endpoint)
            && !string.IsNullOrWhiteSpace(Secret);
    }
}
