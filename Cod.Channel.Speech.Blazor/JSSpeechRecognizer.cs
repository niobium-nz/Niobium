using Microsoft.JSInterop;
using System.Text.Json;

namespace Cod.Channel.Speech.Blazor
{
    public class JSSpeechRecognizer : ISpeechRecognizer, IAsyncDisposable
    {
        private const int maxRetryOnStartFailure = 3;

        private static readonly TimeSpan retryIntervalOnStartFailure = TimeSpan.FromMilliseconds(500);
        private static readonly JsonSerializerOptions options = new(JsonSerializerDefaults.Web);

        private readonly Lazy<Task<IJSObjectReference>> recognizer;
        private readonly Lazy<IEnumerable<IDomainEventHandler<ISpeechRecognizer>>> eventHandlers;

        public JSSpeechRecognizer(IJSRuntime runtime, Lazy<IEnumerable<IDomainEventHandler<ISpeechRecognizer>>> eventHandlers)
        {
            if (!OperatingSystem.IsBrowser())
            {
                throw new ApplicationException(InternalError.NotAcceptable);
            }

            recognizer = new(() => runtime.InvokeAsync<IJSObjectReference>("import", "./_content/Cod.Channel.Speech.Blazor/speech.js").AsTask());

            SpeechRecognizerInterop.Instance = this;
            this.eventHandlers = eventHandlers;
        }

        public bool IsRunning { get; internal set; }

        public bool ContinueOnPrevious { get; private set; }

        public Conversation? Current { get; internal set; }

        public ConversationLine? Preview { get; internal set; }

        public async Task<IEnumerable<InputSourceDevice>> GetInputSourcesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                IJSObjectReference t = await recognizer.Value;
                string result = await t.InvokeAsync<string>("getInputSources", cancellationToken: cancellationToken);
                return Deserialize<InputSourceDevice[]>(result)!;
            }
            catch
            {
                //"Oops! It looks like you're using an unsupported browser. Please try again using the latest version of Google Chrome, Microsoft Edge, or Safari."
                throw new ApplicationException(InternalError.PreconditionFailed);
            }
        }

        public async Task<bool> StartRecognitionAsync(string token, string region, string? deviceID = null, string? language = "en-US", bool translateIntoEnglish = false, bool continueOnPrevious = false, CancellationToken cancellationToken = default)
        {
            return await StartRecognitionAsync(1, token, region, deviceID, language, translateIntoEnglish, continueOnPrevious, cancellationToken);
        }

        protected async Task<bool> StartRecognitionAsync(int attempt, string token, string region, string? deviceID = null, string? language = "en-US", bool translateIntoEnglish = false, bool continueOnPrevious = false, CancellationToken cancellationToken = default)
        {
            if (attempt > maxRetryOnStartFailure)
            {
                return false;
            }

            ContinueOnPrevious = continueOnPrevious;

            try
            {
                IJSObjectReference t = await recognizer.Value;
                IsRunning = await t.InvokeAsync<bool>("startRecognition", cancellationToken, [deviceID ?? string.Empty, language, token, region, translateIntoEnglish]);
                if (!IsRunning)
                {
                    await Task.Delay(retryIntervalOnStartFailure, cancellationToken);
                    return await StartRecognitionAsync(++attempt, token, region, deviceID, language, translateIntoEnglish, continueOnPrevious, cancellationToken);
                }

                return true;
            }
            catch
            {
                IsRunning = false;
                await Task.Delay(retryIntervalOnStartFailure, cancellationToken);
                return await StartRecognitionAsync(++attempt, token, region, deviceID, language, translateIntoEnglish, continueOnPrevious, cancellationToken);
            }
        }

        public async Task StopRecognitionAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                IJSObjectReference t = await recognizer.Value;
                await t.InvokeVoidAsync("stopRecognition");
                ContinueOnPrevious = false;
            }
            finally
            {
                IsRunning = false;
            }
        }

        public void Reset()
        {
            IsRunning = false;
            Current = null;
            Preview = null;
        }

        internal async Task OnChangedAsync(SpeechRecognizerChangedType type)
        {
            await eventHandlers.Value.InvokeAsync(new SpeechRecognizerChangedEventArgs(type));
        }

        private static T Deserialize<T>(string json) where T : class
        {
            return System.Text.Json.JsonSerializer.Deserialize<T>(json, options)!;
        }

        public async ValueTask DisposeAsync()
        {
            if (recognizer.IsValueCreated)
            {
                IJSObjectReference t = await recognizer.Value;
                await t.DisposeAsync();
            }

            GC.SuppressFinalize(this);
        }
    }
}
