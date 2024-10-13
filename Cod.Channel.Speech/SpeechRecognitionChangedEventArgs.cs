namespace Cod.Channel.Speech
{
    public class SpeechRecognitionChangedEventArgs : EventArgs
    {
        public required string SessionID { get; set; }
    }
}
