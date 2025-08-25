# Niobium.Channel.Speech.Blazor

Niobium.Channel.Speech.Blazor provides Blazor UI components and integration for speech recognition and synthesis, enabling voice-driven features in .NET Blazor WebAssembly applications. It builds on Niobium.Channel.Speech to offer seamless browser-based speech capabilities.

## What is this project about?
- Blazor UI components for speech-to-text and text-to-speech.
- Integrates browser speech APIs for use in .NET Blazor apps.
- Works with Niobium.Channel.Speech for shared contracts and logic.
- Enables voice commands, dictation, and accessibility features in Blazor WebAssembly projects.

## Getting Started

### 1. Install the NuGet Package
Add the package to your Blazor WebAssembly project:

```
dotnet add package Niobium.Channel.Speech.Blazor
```

### 2. Register Services in Dependency Injection
In your `Program.cs`:

```csharp
using Niobium.Channel.Speech.Blazor;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.Services.AddSpeechBlazor();
```

### 3. Use Components in Your Blazor Pages

```razor
@using Niobium.Channel.Speech.Blazor.Components

<SpeechRecognizer OnResult="OnSpeechResult" />
<SpeechSynthesizer Text="Hello, world!" />
```

### 4. Handle Speech Events

```csharp
void OnSpeechResult(string text)
{
    // Handle recognized speech
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
