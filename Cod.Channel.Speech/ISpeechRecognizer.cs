namespace Cod.Channel.Speech
{
    public interface ISpeechRecognizer
    {
        bool IsRunning { get; }

        Conversation? Current { get; }

        ConversationLine? Preview { get; }

        event EventHandler? Changed;

        Task<IEnumerable<InputSourceDevice>> GetInputSourcesAsync();

        Task<bool> StartRecognitionAsync(string token, string region, string? deviceID = null, string? language = "en-US", bool translateIntoEnglish = false);

        Task StopRecognitionAsync();
    }
}
