namespace Cod.Channel.Speech
{
    public class SpeechRecognitionCanceledEventArgs : SpeechRecognitionChangedEventArgs
    {
        public required int Reason { get; set; }

        public required SpeechRecognitionCanceledError ErrorCode { get; set; }

        public required string ErrorDetails { get; set; }
    }
}
