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
        private static readonly TimeSpan[] retryIntervalOnStartFailure = [TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30)];
        
        private string? lastInputLanguage;
        private string? lastInputSource;

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
            await StartAsync(inputLanguage, inputSource, false, cancellationToken);
        }

        protected virtual async Task StartAsync(string inputLanguage, string? inputSource, bool resumeOnPrevious, CancellationToken cancellationToken = default)
        {
            if (IsListening)
            {
                return;
            }

            lastInputLanguage = inputLanguage;
            lastInputSource = inputSource;
            var success = false;

            using (loadingStateService.SetBusy(BusyGroups.Speech))
            {
                await OnUpdateAsync(cancellationToken);
                (var sas, var region) = await authenticator.GetSpeechSASAndRegionAsync(cancellationToken);

                for (var i = 0; i < retryIntervalOnStartFailure.Length; i++)
                {
                    success = await recognizer.StartRecognitionAsync(
                        sas,
                        region,
                        deviceID: inputSource,
                        language: inputLanguage,
                        continueOnPrevious: resumeOnPrevious,
                        cancellationToken: cancellationToken);

                    if (success)
                    {
                        break;
                    }
                    else
                    {
                        await Task.Delay(retryIntervalOnStartFailure[i], cancellationToken);
                    }
                }
            }
            await OnUpdateAsync(cancellationToken);

            if (!success)
            {
                throw new ApplicationException(InternalError.NetworkFailure);
            }
        }

        public async Task ResumeAsync(CancellationToken cancellationToken = default)
        {
            if (lastInputLanguage != null)
            {
                await StartAsync(lastInputLanguage, lastInputSource, true, cancellationToken);
            }
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

        public override async Task HandleCoreAsync(SpeechRecognizerChangedEventArgs e, CancellationToken cancellationToken = default)
        {
            if (e.Type == SpeechRecognizerChangedType.Canceled)
            {
                await ResumeAsync(cancellationToken);
            }
            else
            {
                await OnUpdateAsync(cancellationToken);
            }
        }

        private async Task OnUpdateAsync(CancellationToken cancellationToken)
            => await eventHandlers.Value.InvokeAsync(new SpeechServiceUpdatedEventArgs(), cancellationToken: cancellationToken);
    }
}
