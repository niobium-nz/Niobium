namespace Cod.Platform.OpenAI
{
    public interface IOpenAIService
    {
        Task<OpenAIConversationAnalysisResult?> AnalyzeSOAPAsync(string id, int kind, string conversation, string outputLanguage, CancellationToken cancellationToken);
    }
}
