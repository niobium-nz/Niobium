# Niobium.Platform.Speech

Niobium.Platform.Speech provides abstractions, dependency injection modules, and helpers for integrating speech-to-text and related speech services into .NET applications. It enables secure, role-based, and personalized access to speech transcription and analysis features.

## What is this project about?
- Implements DI modules for registering speech service controls and entitlement management.
- Provides role-based and personalized entitlement descriptors for secure access to speech services.
- Supplies helpers for signature issuance and resource control.
- Used by consumer projects to standardize and secure speech service integration.

## Getting Started

### 1. Install the NuGet Package
Add the package to your .NET project:

```
dotnet add package Niobium.Platform.Speech
```

### 2. Register Speech Services in Dependency Injection
In your DI setup:

```csharp
using Niobium.Platform.Speech;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddSpeech(options =>
{
    options.SubscriptionKey = "<your-speech-service-key>";
    options.Region = "<your-region>";
    // Configure additional options as needed
});
```

### 3. Grant Entitlements
- **Role-based Transcribe Entitlement:**
  ```csharp
  services.GrantSpeechTranscribeEntitlementTo(
      sp => "MyRole");
  ```
- **Personalized Transcribe Entitlement:**
  ```csharp
  services.GrantSpeechPersonalizedTranscribeEntitlementTo(
      sp => "MyRole");
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
