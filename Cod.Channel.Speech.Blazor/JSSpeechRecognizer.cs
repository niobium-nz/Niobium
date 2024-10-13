using Microsoft.JSInterop;
using System.Text.Json;

namespace Cod.Channel.Speech.Blazor
{
    public class JSSpeechRecognizer : ISpeechRecognizer, IAsyncDisposable
    {
        private static readonly JsonSerializerOptions options = new(JsonSerializerDefaults.Web);
        private readonly Lazy<Task<IJSObjectReference>> translator;

        public JSSpeechRecognizer(IJSRuntime runtime)
        {
            if (!OperatingSystem.IsBrowser())
            {
                throw new NotSupportedException();
            }

            translator = new(() => runtime.InvokeAsync<IJSObjectReference>(
                "import", "./_content/Cod.Channel.Speech.Blazor/speech.js").AsTask());

            SpeechRecognizerInterop.Instance = this;
        }

        public bool IsRunning { get; internal set; }

        public Conversation? Current { get; internal set; }

        public ConversationLine? Preview { get; internal set; }

        public event EventHandler? Changed;

        public async Task<IEnumerable<InputSourceDevice>> GetInputSourcesAsync()
        {
            var t = await translator.Value;
            var result = await t.InvokeAsync<string>("getInputSources");
            return Deserialize<InputSourceDevice[]>(result)!;
        }

        public async Task<bool> StartRecognitionAsync(string token, string region, string? deviceID = null, string? language = "en-US", bool translateIntoEnglish = false)
        {
            var t = await translator.Value;
            var result = await t.InvokeAsync<bool>("startRecognition", [deviceID ?? string.Empty, language, token, region, translateIntoEnglish]);
            if (result)
            {
                IsRunning = true;
            }
            else
            {
                IsRunning = false;
                Current = new Conversation
                {
                    CreatedAt = DateTimeOffset.Now,
                    ID = Guid.NewGuid().ToString("N"),
                    Lines = [],
                    ErrorMessage = "Oops! It looks like the conversation couldn't start because of an unsupported browser. Please try again using the latest version of Google Chrome, Microsoft Edge, or Safari."
                };
            }

            return IsRunning;
        }

        public async Task StopRecognitionAsync()
        {
            var t = await translator.Value;
            await t.InvokeVoidAsync("stopRecognition");
            IsRunning = false;
        }

        internal void OnChanged()
        {
            Changed?.Invoke(null, EventArgs.Empty);
        }

        private static T Deserialize<T>(string json) where T : class
        {
            return System.Text.Json.JsonSerializer.Deserialize<T>(json, options)!;
        }

        public async ValueTask DisposeAsync()
        {
            if (translator.IsValueCreated)
            {
                var t = await translator.Value;
                await t.DisposeAsync();
            }
        }
    }
}
