namespace Cod.Cloud.Azure.OpenAI
{
    public interface IOpenAIService
    {
        Task<OpenAIConversationAnalysisResult?> AnalyzeSOAPAsync(string id, string conversation, CancellationToken cancellationToken);
    }
}
