using Microsoft.JSInterop;
using System.Text.Json;

namespace Cod.Channel.Speech.Blazor
{
    public static class SpeechRecognizerInterop
    {
        private static readonly TimeSpan silenceTimeout = TimeSpan.FromMinutes(1);
        private static readonly JsonSerializerOptions options = new(JsonSerializerDefaults.Web);

        public static JSSpeechRecognizer? Instance { get; set; }

        [JSInvokable]
        public static async Task OnSpeechRecognizerChangedAsync(string eventName, string parameter)
        {
            if (Instance == null || string.IsNullOrWhiteSpace(eventName) || string.IsNullOrWhiteSpace(parameter))
            {
                return;
            }

            switch (eventName)
            {
                case "onSessionStarted":
                    DestoryCurrentSession();
                    Instance.IsRunning = true;
                    await Instance.OnChangedAsync(SpeechRecognizerChangedType.SessionStarted);
                    break;
                case "onSessionStopped":
                    Instance.IsRunning = false;
                    await Instance.OnChangedAsync(SpeechRecognizerChangedType.SessionStopped);
                    break;
                case "onCanceled":
                    var canceled = Deserialize<SpeechRecognitionCanceledEventArgs>(parameter);
                    CreateNewSession(canceled.SessionID);
                    Instance.Current!.ErrorMessage = canceled.ErrorDetails;
                    Instance.IsRunning = false;
                    await Instance.OnChangedAsync(SpeechRecognizerChangedType.Canceled);
                    break;
                case "onRecognizing":
                    var recognizing = Deserialize<SpeechRecognitionEventArgs>(parameter);
                    CreateNewSession(recognizing.SessionID);
                    Instance.Preview = ExtractLine(recognizing);
                    await Instance.OnChangedAsync(SpeechRecognizerChangedType.Recognizing);
                    break;
                case "onRecognized":
                    var recognized = Deserialize<SpeechRecognitionEventArgs>(parameter);
                    CreateNewSession(recognized.SessionID);
                    Instance.Preview = null;
                    ConversationLine? line = ExtractLine(recognized);
                    if (line != null)
                    {
                        Instance.Current!.Lines.Add(line);
                        await Instance.OnChangedAsync(SpeechRecognizerChangedType.Recognized);
                    }
                    else
                    {
                        await StopRecognitionIfExceedSilentTimeoutAsync();
                    }
                    break;
            }
        }

        private async static Task StopRecognitionIfExceedSilentTimeoutAsync()
        {
            if (Instance?.Current == null)
            {
                return;
            }

            bool shouldStop;
            if (Instance.Current.Lines.Count == 0)
            {
                shouldStop = DateTimeOffset.Now - Instance.Current.CreatedAt > silenceTimeout;
            }
            else
            {
                var lastLineCreatedAt = Instance.Current.Lines.OrderByDescending(l => l.CreatedAt).First().CreatedAt;
                shouldStop = DateTimeOffset.Now - lastLineCreatedAt > silenceTimeout;
            }

            if (shouldStop)
            {
                Instance.Current.ErrorMessage = "The conversation has timed out due to silence, or there might be an issue with your audio. Please check your audio input settings and start a new conversation.";
                await Instance.StopRecognitionAsync();
                await Instance.OnChangedAsync(SpeechRecognizerChangedType.Recognized);
            }
        }

        private static ConversationLine? ExtractLine(SpeechRecognitionEventArgs e)
        {
            if (string.IsNullOrEmpty(e.Result.Text))
            {
                return null;
            }

            var line = new ConversationLine
            {
                Text = e.Result.Text,
                CreatedAt = DateTimeOffset.Now,
            };

            if (e.Result.Translations != null)
            {
                var translation = e.Result.Translations.SingleOrDefault(t => t.Language == "en");
                if (translation != null)
                {
                    line.Translation = translation.Text;
                    line.TargetLanguage = translation.Language;
                }
            }

            return line;
        }

        private static void CreateNewSession(string sessionID)
        {
            if (Instance != null)
            {
                Instance.Current ??= new Conversation
                {
                    CreatedAt = DateTimeOffset.Now,
                    ID = sessionID,
                    Lines = []
                };
            }
        }

        private static void DestoryCurrentSession()
        {
            if (Instance != null)
            {
                Instance.Preview = null;
                Instance.Current = null;
            }

        }

        private static T Deserialize<T>(string json) where T : class
        {
            return System.Text.Json.JsonSerializer.Deserialize<T>(json, options)!;
        }
    }
}
