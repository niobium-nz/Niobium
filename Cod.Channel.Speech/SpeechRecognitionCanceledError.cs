namespace Cod.Channel.Speech
{
    public enum SpeechRecognitionCanceledError : int
    {
        /// <summary>
        /// Indicates that no error occurred during speech recognition.
        /// </summary>
        NoError,

        /// <summary>
        /// Indicates an authentication error.
        /// </summary>
        AuthenticationFailure,

        /// <summary>
        /// Indicates that one or more recognition parameters are invalid.
        /// </summary>
        BadRequestParameters,

        /// <summary>
        /// Indicates that the number of parallel requests exceeded the number of allowed concurrent transcriptions for the subscription.
        /// </summary>
        TooManyRequests,

        /// <summary>
        /// Indicates a connection error.
        /// </summary>
        ConnectionFailure,

        /// <summary>
        /// Indicates a time-out error when waiting for response from service.
        /// </summary>
        ServiceTimeout,

        /// <summary>
        /// Indicates that an error is returned by the service.
        /// </summary>
        ServiceError,

        /// <summary>
        /// Indicates an unexpected runtime error.
        /// </summary>
        RuntimeError,

        /// <summary>
        /// Indicates an quota overrun on existing key.
        /// </summary>
        Forbidden,
    }
}
