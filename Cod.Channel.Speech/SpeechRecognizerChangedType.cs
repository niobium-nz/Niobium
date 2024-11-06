namespace Cod.Channel.Speech
{
    public enum SpeechRecognizerChangedType
    {
        None = 0,

        SessionStarted = 1,

        SessionStopped = 2,

        Canceled = 3,

        Recognizing = 4,

        Recognized = 5,
    }
}
