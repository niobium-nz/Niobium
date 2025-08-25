# Niobium.Channel.Speech

Niobium.Channel.Speech provides core abstractions, services, and helpers for speech recognition and synthesis in .NET applications. It enables voice-driven features, such as speech-to-text and text-to-speech, and is designed for integration with Blazor and other .NET projects.

## What is this project about?
- Defines interfaces and services for speech recognition and synthesis.
- Supports extensible speech providers and event-driven speech workflows.
- Used by Niobium.Channel.Speech.Blazor for Blazor UI integration.
- Enables voice commands, dictation, and accessibility features in .NET apps.

## Getting Started

### 1. Install the NuGet Package
Add the package to your .NET project:

```
dotnet add package Niobium.Channel.Speech
```

### 2. Register Services in Dependency Injection
In your `Program.cs` or `Startup.cs`:

```csharp
using Niobium.Channel.Speech;

builder.Services.AddSpeechServices(options =>
{
    // Configure speech options if needed
});
```

### 3. Use Speech Services in Your Application

```csharp
using Niobium.Channel.Speech;

public class MyComponent
{
    private readonly ISpeechRecognizer _recognizer;
    public MyComponent(ISpeechRecognizer recognizer)
    {
        _recognizer = recognizer;
    }
    // Use _recognizer to start/stop recognition
}
```

## Contributing

Contributions are welcome! To contribute:
1. Fork the repository
2. Create a feature branch
3. Make your changes with clear commit messages
4. Submit a pull request

Please ensure your code follows the existing style and includes appropriate tests and documentation.

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.
