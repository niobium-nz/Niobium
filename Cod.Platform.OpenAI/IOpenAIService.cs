namespace Cod.Platform.OpenAI
{
    public interface IOpenAIService
    {
        Task<OpenAIConversationAnalysisResult?> AnalyzeSOAPAsync(string id, string conversation, CancellationToken cancellationToken);
    }
}
