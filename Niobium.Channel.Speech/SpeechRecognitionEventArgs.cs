namespace Niobium.Channel.Speech
{
    public class SpeechRecognitionEventArgs : SpeechRecognitionChangedEventArgs
    {
        public required long Offset { get; set; }

        public required SpeechRecognitionResult Result { get; set; }
    }
}
