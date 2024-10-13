export async function getInputSources() {
    var result = [];
    if (navigator && navigator.mediaDevices) {
        try {
            await navigator.mediaDevices.getUserMedia({ audio: true });

            if (navigator.mediaDevices.enumerateDevices) {
                var devices = await navigator.mediaDevices.enumerateDevices();
                for (const device of devices) {
                    if (device.kind === "audioinput") {
                        if (device.deviceId) {
                            result.push({
                                deviceId: device.deviceId,
                                deviceLabel: device.label
                            });
                        }
                    }
                }
            }
        } catch (e) {
        }        
    }

    return JSON.stringify(result);
}

async function onChanged(eventName, parameter) {
    await DotNet.invokeMethodAsync("Cod.Channel.Speech.Blazor", "OnSpeechRecognizerChangedAsync", eventName, JSON.stringify(parameter));
}

async function onRecognizing(sender, recognitionEventArgs) {
    var translations = undefined;
    if (recognitionEventArgs.result && recognitionEventArgs.result.json) {
        var result = JSON.parse(recognitionEventArgs.result.json).privTranslationHypothesis;
        if (result && result.Translation) {
            translations = result.Translation.Translations;
        }
    }

    var result = parseRecognitionResult(recognitionEventArgs, translations);
    await onChanged("onRecognizing", result);
}

function parseRecognitionResult(recognitionEventArgs, translations) {
    var noMatchReason = undefined;
    if (recognitionEventArgs.result.reason == window.SpeechSDK.ResultReason.NoMatch) {
        var noMatchDetail = window.SpeechSDK.NoMatchDetails.fromResult(recognitionEventArgs.result);
        noMatchReason = noMatchDetail.reason;
    }

    return {
        sessionId: recognitionEventArgs.sessionId,
        offset: recognitionEventArgs.offset,
        result: {
            duration: recognitionEventArgs.result.duration,
            language: recognitionEventArgs.result.language,
            languageDetectionConfidence: recognitionEventArgs.result.languageDetectionConfidence,
            offset: recognitionEventArgs.result.offset,
            reason: recognitionEventArgs.result.reason,
            resultId: recognitionEventArgs.result.resultId,
            speakerId: recognitionEventArgs.result.speakerId,
            text: recognitionEventArgs.result.text,
            translations: translations,
            noMatchReason: noMatchReason,
        }
    }
}

async function onRecognized(sender, recognitionEventArgs) {
    var translations = undefined;
    if (recognitionEventArgs.result && recognitionEventArgs.result.json) {
        var result = JSON.parse(recognitionEventArgs.result.json).privTranslationPhrase;
        if (result && result.Translation) {
            translations = result.Translation.Translations;
        }
    }

    var result = parseRecognitionResult(recognitionEventArgs, translations);
    await onChanged("onRecognized", result);
}

async function onSessionStarted(sender, sessionEventArgs) {
    await onChanged("onSessionStarted", { sessionId: sessionEventArgs.sessionId } );
}

async function onSessionStopped(sender, sessionEventArgs) {
    await stopRecognition();
    await onChanged("onSessionStopped", { sessionId: sessionEventArgs.sessionId });
}

async function onCanceled(sender, cancellationEventArgs) {
    await stopRecognition();
    await onChanged("onCanceled", {
        sessionId: cancellationEventArgs.sessionId,
        reason: cancellationEventArgs.reason,
        errorCode: cancellationEventArgs.errorCode,
        errorDetails: cancellationEventArgs.errorDetails
    });
}

function isRunning() {
    if (window && window.currentRecognizer) {
        return true;
    } else {
        return false;
    }
}

export function startRecognition(deviceID, language, token, region, translateIntoEnglish) {
    if (!window || !window.SpeechSDK) {
        return false;
    }

    if (isRunning()) {
        return false;
    }

    try {
        var audioConfig;
        if (deviceID) {
            audioConfig = window.SpeechSDK.AudioConfig.fromMicrophoneInput(deviceID);
        } else {
            audioConfig = window.SpeechSDK.AudioConfig.fromDefaultMicrophoneInput();
        }

        var speechConfig = window.SpeechSDK.SpeechTranslationConfig.fromAuthorizationToken(token, region);
        speechConfig.speechRecognitionLanguage = language;

        if (!audioConfig || !speechConfig) {
            return false;
        }

        if (translateIntoEnglish) {
            speechConfig.addTargetLanguage("en-US");
        }

        window.currentRecognizer = translateIntoEnglish ? new window.SpeechSDK.TranslationRecognizer(speechConfig, audioConfig)
            : new window.SpeechSDK.SpeechRecognizer(speechConfig, audioConfig);
        window.currentRecognizer.telemetryEnabled = false;
        window.currentRecognizer.recognizing = onRecognizing;
        window.currentRecognizer.recognized = onRecognized;
        window.currentRecognizer.canceled = onCanceled;
        window.currentRecognizer.sessionStarted = onSessionStarted;
        window.currentRecognizer.sessionStopped = onSessionStopped;
        window.currentRecognizer.startContinuousRecognitionAsync();
        return true;
    } catch (e) {
        return false;
    }    
}

export async function stopRecognition() {
    if (isRunning()) {
        await window.currentRecognizer.stopContinuousRecognitionAsync(
            function () {
                try {
                    if (window.currentRecognizer) {
                        window.currentRecognizer.close();
                    }
                    window.currentRecognizer = undefined;
                } catch (e) {
                }

            },
            function (err) {
                try {
                    if (window.currentRecognizer) {
                        window.currentRecognizer.close();
                    }
                    window.currentRecognizer = undefined;
                } catch (e) {
                }

            }
        );
        window.currentRecognizer = undefined;
    }
}