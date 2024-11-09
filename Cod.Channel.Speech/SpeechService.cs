using Cod.Identity;

namespace Cod.Channel.Speech
{
    internal class SpeechService(
        ISpeechRecognizer recognizer,
        ILoadingStateService loadingStateService,
        IAuthenticator authenticator,
        Lazy<IEnumerable<IDomainEventHandler<ISpeechService>>> eventHandlers)
        : DomainEventHandler<ISpeechRecognizer, SpeechRecognizerChangedEventArgs>, ISpeechService
    {
        public bool IsListening => recognizer.IsRunning;

        public Conversation? Current => recognizer.Current;

        public ConversationLine? Preview => recognizer.Preview;

        public IEnumerable<InputSourceDevice> InputSources { get; private set; } = [];

        public async Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            InputSources = await recognizer.GetInputSourcesAsync(cancellationToken);
            await OnUpdateAsync(cancellationToken);
        }

        public virtual async Task StartAsync(string inputLanguage, string? inputSource, CancellationToken cancellationToken = default)
        {
            using (loadingStateService.SetBusy(BusyGroups.Speech))
            {
                await OnUpdateAsync(cancellationToken);
                (var sas, var region) = await authenticator.GetSpeechSASAndRegionAsync(cancellationToken);
                await recognizer.StartRecognitionAsync(sas, region, deviceID: inputSource, language: inputLanguage, cancellationToken: cancellationToken);
            }
            await OnUpdateAsync(cancellationToken);
        }

        public async Task StopAsync(CancellationToken cancellationToken = default)
        {
            await recognizer.StopRecognitionAsync(cancellationToken);
            await OnUpdateAsync(cancellationToken);

            var hasConversation = Current != null && Current.Lines.Count > 0 && Current.Lines.Any(l => !string.IsNullOrWhiteSpace(l.Text));
            if (hasConversation)
            {
                var conversation = string.Join('\n', Current!.Lines.Where(l => !string.IsNullOrWhiteSpace(l.Text)).Select(l => l.Text));
                await eventHandlers.Value.InvokeAsync(new SpeechServiceRecognizedEventArgs(Current.ID, conversation), cancellationToken);
            }
        }

        public void Reset() => recognizer.Reset();

        public override async Task HandleAsync(SpeechRecognizerChangedEventArgs e, CancellationToken cancellationToken)
            => await OnUpdateAsync(cancellationToken);

        private async Task OnUpdateAsync(CancellationToken cancellationToken)
            => await eventHandlers.Value.InvokeAsync(new SpeechServiceUpdatedEventArgs(), cancellationToken: cancellationToken);
    }
}
