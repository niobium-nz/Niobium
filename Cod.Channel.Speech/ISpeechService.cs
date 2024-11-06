namespace Cod.Channel.Speech
{
    public interface ISpeechService
    {
        bool IsBusy { get; }

        bool IsRunning { get; }

        Conversation? Current { get; }

        ConversationLine? Preview { get; }

        IEnumerable<InputSourceDevice> InputSources { get; }

        Task InitializeAsync(CancellationToken cancellationToken = default);

        Task StartAsync(string inputLanguage, string? inputSource, CancellationToken cancellationToken = default);

        Task StopAsync(CancellationToken cancellationToken = default);
    }

    public class SpeechServiceRecognizedEventArgs(string id, string conversation)
    {
        public string ID { get; } = id;

        public string Conversation { get; } = conversation;
    }

    public class SpeechServiceUpdatedEventArgs { }
}
