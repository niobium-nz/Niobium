# Cod.Platform.OpenAI

Cod.Platform.OpenAI provides abstractions, dependency injection modules, and helpers for integrating OpenAI-powered language models (such as GPT) into .NET applications. It enables secure, configurable, and extensible access to OpenAI APIs for tasks like conversation analysis, summarization, and more.

## What is this project about?
- Defines interfaces and services for interacting with OpenAI APIs.
- Provides DI modules for registering OpenAI services and configuration.
- Supports system prompts, conversation analysis, and result formatting.
- Used by consumer projects to easily add AI-powered features.

## Getting Started

### 1. Install the NuGet Package
Add the package to your .NET project:

```
dotnet add package Cod.Platform.OpenAI
```

### 2. Register OpenAI Services in Dependency Injection
In your application's startup or DI configuration:

```csharp
using Cod.Platform.OpenAI;

builder.Services.AddOpenAI(options =>
{
    options.ApiKey = "<your-openai-api-key>";
    options.SystemPrompts = new()
    {
        { (int)AnalysisKind.Minutes, "<system prompt for minutes>" },
        { (int)AnalysisKind.SOAPNotes, "<system prompt for SOAP notes>" },
    };
});
```

### 3. Use OpenAI Services in Your Application

```csharp
using Cod.Platform.OpenAI;

public class MyService
{
    private readonly IOpenAIService _openAI;
    public MyService(IOpenAIService openAI)
    {
        _openAI = openAI;
    }
    // Use _openAI to analyze conversations, generate text, etc.
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
