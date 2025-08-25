namespace Niobium.Channel.Speech
{
    public interface ISpeechRecognizer
    {
        bool IsRunning { get; }

        Conversation? Current { get; }

        ConversationLine? Preview { get; }

        Task<IEnumerable<InputSourceDevice>> GetInputSourcesAsync(CancellationToken cancellationToken = default);

        Task<bool> StartRecognitionAsync(string token, string region, string? deviceID = null, string? language = "en-US", bool translateIntoEnglish = false, bool continueOnPrevious = false, CancellationToken cancellationToken = default);

        Task StopRecognitionAsync(CancellationToken cancellationToken = default);

        void Reset();
    }
}
