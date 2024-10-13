namespace Cod.Channel.Speech
{
    public enum SpeechRecognitionResultReason : int
    {
        /// <summary>
        /// Indicates speech could not be recognized. More details can be found using the NoMatchDetails object.
        /// </summary>
        NoMatch,

        /// <summary>
        /// Indicates that the recognition was canceled. More details can be found using the CancellationDetails object.
        /// </summary>
        Canceled,

        /// <summary>
        /// Indicates the speech result contains hypothesis text.
        /// </summary>
        RecognizingSpeech,

        /// <summary>
        /// Indicates the speech result contains final text that has been recognized. Speech Recognition is now complete for this phrase.
        /// </summary>
        RecognizedSpeech,

        /// <summary>
        /// Indicates the speech result contains a finalized acceptance of a provided keyword. Speech recognition will continue unless otherwise configured.
        /// </summary>
        RecognizedKeyword,

        /// <summary>
        /// Indicates the intent result contains hypothesis text and intent.
        /// </summary>
        RecognizingIntent,

        /// <summary>
        /// Indicates the intent result contains final text and intent. Speech Recognition and Intent determination are now complete for this phrase.
        /// </summary>
        RecognizedIntent,

        /// <summary>
        /// Indicates the translation result contains hypothesis text and its translation(s).
        /// </summary>
        TranslatingSpeech,

        /// <summary>
        /// Indicates the translation result contains final text and corresponding translation(s). Speech Recognition and Translation are now complete for this phrase.
        /// </summary>
        TranslatedSpeech,

        /// <summary>
        /// Indicates the synthesized audio result contains a non-zero amount of audio data
        /// </summary>
        SynthesizingAudio,

        /// <summary>
        /// Indicates the synthesized audio is now complete for this phrase.
        /// </summary>
        SynthesizingAudioCompleted,

        /// <summary>
        /// Indicates the speech synthesis is now started
        /// </summary>
        SynthesizingAudioStarted,

        /// <summary>
        /// Indicates the voice profile is being enrolled and customers need to send more audio to create a voice profile.
        /// </summary>
        EnrollingVoiceProfile,

        /// <summary>
        /// Indicates the voice profile has been enrolled.
        /// </summary>
        EnrolledVoiceProfile,

        /// <summary>
        /// Indicates successful identification of some speakers.
        /// </summary>
        RecognizedSpeakers,

        /// <summary>
        /// Indicates successfully verified one speaker.
        /// </summary>
        RecognizedSpeaker,

        /// <summary>
        /// Indicates a voice profile has been reset successfully.
        /// </summary>
        ResetVoiceProfile,

        /// <summary>
        /// Indicates a voice profile has been deleted successfully.
        /// </summary>
        DeletedVoiceProfile,

        /// <summary>
        /// Indicates synthesis voices list has been successfully retrieved.
        /// </summary>
        VoicesListRetrieved,

        /// <summary>
        /// Indicates the transcription result contains hypothesis text and its translation(s) for other participants in the conversation.
        /// </summary>
        TranslatingParticipantSpeech,

        /// <summary>
        /// Indicates the transcription result contains final text and corresponding translation(s) for other participants in the conversation. Speech Recognition and Translation are now complete for this phrase.
        /// </summary>
        TranslatedParticipantSpeech,

        /// <summary>
        /// Indicates the transcription result contains the instant message and corresponding translation(s).
        /// </summary>
        TranslatedInstantMessage,

        /// <summary>
        /// Indicates the transcription result contains the instant message for other participants in the conversation and corresponding translation(s).
        /// </summary>
        TranslatedParticipantInstantMessage,
    }
}
