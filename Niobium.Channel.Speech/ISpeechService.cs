using Niobium.Messaging;

namespace Niobium.Channel.Speech
{
    public interface ISpeechService
    {
        bool IsListening { get; }

        Conversation? Current { get; }

        ConversationLine? Preview { get; }

        IEnumerable<InputSourceDevice> InputSources { get; }

        Task InitializeAsync(CancellationToken cancellationToken = default);

        Task StartAsync(string inputLanguage, string? inputSource, CancellationToken cancellationToken = default);

        Task StopAsync(CancellationToken cancellationToken = default);

        void Reset();
    }

    public class SpeechServiceRecognizedEventArgs : DomainEvent
    {
        public SpeechServiceRecognizedEventArgs(string id, string conversation)
        {
            ID = id;
            Conversation = conversation;
        }

        public string Conversation { get; }
    }

    public class SpeechServiceUpdatedEventArgs { }
}
