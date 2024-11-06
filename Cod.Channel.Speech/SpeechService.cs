namespace Cod.Channel.Speech
{
    internal class SpeechService(
        ISpeechRecognizer recognizer,
        ILoadingStateService loadingStateService,
        ICommand<SpeechRecognizeCommandParameter, bool> command,
        Lazy<IEnumerable<IDomainEventHandler<ISpeechService>>> eventHandlers)
        : DomainEventHandler<ISpeechRecognizer, SpeechRecognizerChangedEventArgs>, ISpeechService
    {
        public bool IsBusy => loadingStateService.IsBusy(BusyGroups.Speech);

        public bool IsRunning { get; private set; }

        public Conversation? Current { get; private set; }

        public ConversationLine? Preview { get; private set; }

        public IEnumerable<InputSourceDevice> InputSources { get; private set; } = [];

        public async Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            InputSources = await recognizer.GetInputSourcesAsync(cancellationToken);
            await OnUpdateAsync(cancellationToken);
        }

        public virtual async Task StartAsync(string inputLanguage, string? inputSource, CancellationToken cancellationToken = default)
        {
            Preview = null;
            Current = null;

            await command.ExecuteAsync(new SpeechRecognizeCommandParameter
            {
                InputLanguage = inputLanguage,
                InputSource = inputSource,
            }, cancellationToken);

            IsRunning = recognizer.IsRunning;
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

        public override async Task HandleAsync(SpeechRecognizerChangedEventArgs e, CancellationToken cancellationToken)
        {
            await OnUpdateAsync(cancellationToken);
        }

        private async Task OnUpdateAsync(CancellationToken cancellationToken)
        {
            IsRunning = recognizer.IsRunning;
            Preview = recognizer.Preview;
            Current = recognizer.Current;
            await eventHandlers.Value.InvokeAsync(new SpeechServiceUpdatedEventArgs(), cancellationToken: cancellationToken);
        }
    }
}
