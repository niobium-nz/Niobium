namespace Cod.Channel.Speech
{
    public enum SpeechRecognitionNoMatchReason : int
    {
        /// <summary>
        /// Indicates that speech was detected, but not recognized.
        /// </summary>
        NotRecognized,

        /// <summary>
        /// Indicates that the start of the audio stream contained only silence, and the service timed out waiting for speech.
        /// </summary>
        InitialSilenceTimeout,

        /// <summary>
        /// Indicates that the start of the audio stream contained only noise, and the service timed out waiting for speech.
        /// </summary>
        InitialBabbleTimeout,
    }
}
