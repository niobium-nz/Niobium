namespace Cod.Channel.Speech
{
    public abstract class SpeechViewModel : IDisposable
    {
        private bool disposed;
        private readonly ISpeechRecognizer recognizer;
        private readonly ILoadingStateService loadingStateService;
        private readonly ICommand<SpeechRecognizeCommandParameter, bool> command;

        public SpeechViewModel(
            ISpeechRecognizer recognizer,
            ILoadingStateService loadingStateService,
            ICommand<SpeechRecognizeCommandParameter, bool> command)
        {
            recognizer.Changed += SpeechRecognizer_Changed;
            this.recognizer = recognizer;
            this.loadingStateService = loadingStateService;
            this.command = command;
        }

        public bool IsBusy => loadingStateService.IsBusy(BusyGroups.Speech);

        public bool IsRunning { get; private set; }

        public Conversation? Current { get; private set; }

        public ConversationLine? Preview { get; private set; }

        public IEnumerable<InputSourceDevice> InputSources { get; private set; } = [];

        public event EventHandler? Changed;

        public async Task InitializeAsync()
        {
            InputSources = await recognizer.GetInputSourcesAsync();
            Update();
        }

        public virtual async Task StartAsync(string inputLanguage, string? inputSource)
        {
            Preview = null;
            Current = null;

            await command.ExecuteAsync(new SpeechRecognizeCommandParameter
            {
                InputLanguage = inputLanguage,
                InputSource = inputSource,
            });

            IsRunning = recognizer.IsRunning;
        }

        public async Task StopAsync()
        {
            await recognizer.StopRecognitionAsync();
            Update();

            var hasConversation = Current != null && Current.Lines.Count > 0 && Current.Lines.Any(l => !string.IsNullOrWhiteSpace(l.Text));

            if (hasConversation)
            {
                var conversation = string.Join('\n', Current!.Lines.Where(l => !string.IsNullOrWhiteSpace(l.Text)).Select(l => l.Text));
                using (loadingStateService.SetBusy(BusyGroups.Speech))
                { 
                    var isSuccess = await OnConversationRecognized(Current.ID, conversation);
                    if (!isSuccess)
                    {
                        //TODO resubmit button?
                        Current.ErrorMessage = "The conversation analysis has failed due to network issue. Please check your internet connectivity.";
                    }
                }
            }
        }

        protected abstract Task<bool> OnConversationRecognized(string id, string conversation);

        private void Update()
        {
            IsRunning = recognizer.IsRunning;
            Preview = recognizer.Preview;
            Current = recognizer.Current;
        }

        private void SpeechRecognizer_Changed(object? sender, EventArgs e)
        {
            Update();
            OnChanged();
        }

        protected virtual void OnChanged()
        {
            Changed?.Invoke(this, EventArgs.Empty);
        }

        public void Dispose()
        {
            if (!disposed)
            {
                recognizer.Changed -= SpeechRecognizer_Changed;
                disposed = true;
            }

            GC.SuppressFinalize(this);
        }
    }
}
