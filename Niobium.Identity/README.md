# Niobium.Identity

Niobium.Identity provides core abstractions, helpers, and types for authentication, authorization, and identity management in the Niobium ecosystem. It defines contracts for login flows, authentication results, and identity utilities for business applications.

## What is this project about?
- Defines types for login results, authentication results, and identity helpers.
- Supplies enums and contracts for authentication kinds and flows.
- Used as a foundational dependency by Niobium.Channel.Identity and other Niobium.* packages for secure authentication and identity management.

## Getting Started

### 1. Install the NuGet Package
Add the package to your .NET project:

```
dotnet add package Niobium.Identity
```

### 2. Use the Types and Helpers in Your Code
Use the provided types and helpers for authentication and identity management:

```csharp
using Niobium.Identity;

var identity = IdentityHelper.BuildIdentity(appGuid, "user@example.com");
var isValid = IdentityHelper.TryParseAppAndUserName(identity, out var app, out var username);
```

### 3. Integrate with Other Niobium Packages
Niobium.Identity is automatically referenced by Niobium.Channel.Identity and other Niobium.* packages. Just add the relevant Niobium.* packages to your project and use the shared contracts and helpers.

## Contributing

Contributions are welcome! To contribute:
1. Fork the repository
2. Create a feature branch
3. Make your changes with clear commit messages
4. Submit a pull request

Please ensure your code follows the existing style and includes appropriate tests and documentation.

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.
