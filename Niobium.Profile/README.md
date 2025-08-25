# Niobium.Profile

Niobium.Profile provides core abstractions and base types for user profile data in the Niobium ecosystem. It defines contracts for profile entities, preferences, and extensibility points for business applications.

## What is this project about?
- Defines interfaces and base types for user profile data, preferences, and extensibility.
- Used as a foundational dependency by Niobium.Channel.Profile and other Niobium.* packages.
- Ensures a consistent contract for profile entities and related business logic.

## Getting Started

### 1. Install the NuGet Package
Add the package to your .NET project:

```
dotnet add package Niobium.Profile
```

### 2. Use the Abstractions in Your Code
Implement the interfaces and use the base types in your domain and infrastructure code:

```csharp
using Niobium.Profile;

public class UserProfile : IProfile
{
    public string UserId { get; set; }
    // ... other properties ...
}
```

### 3. Integrate with Other Niobium Packages
Niobium.Profile is automatically referenced by Niobium.Channel.Profile and other Niobium.* packages. Just add the relevant Niobium.* packages to your project and use the shared contracts.

## Contributing

Contributions are welcome! To contribute:
1. Fork the repository
2. Create a feature branch
3. Make your changes with clear commit messages
4. Submit a pull request

Please ensure your code follows the existing style and includes appropriate tests and documentation.

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.
