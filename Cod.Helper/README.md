# Cod.Helper

Cod.Helper provides utility classes and helpers for cryptography, encoding, and other common operations in the Cod ecosystem. It includes reusable helpers for RSA encryption, hashing, and more.

## What is this project about?
- Implements helpers for RSA encryption/decryption, signing, and verification.
- Provides utility methods for encoding, hashing, and other common tasks.
- Used by Cod.Channel, Cod.Identity, and other Cod.* packages for secure and efficient operations.

## Getting Started

### 1. Install the NuGet Package
Add the package to your .NET project:

```
dotnet add package Cod.Helper
```

### 2. Use the Helpers in Your Code
Use the provided helpers for cryptography and utility operations:

```csharp
using Cod;

using var rsa = new RSAHelper(RSAType.RSA2, isOpenSSL: true, privateKey: "...", publicKey: "...");
string signature = rsa.Sign("data");
bool isValid = rsa.Verify("data", signature);
```

### 3. Integrate with Other Cod Packages
Cod.Helper is automatically referenced by Cod.Channel and other Cod.* packages. Just add the relevant Cod.* packages to your project and use the shared helpers.

## Contributing

Contributions are welcome! To contribute:
1. Fork the repository
2. Create a feature branch
3. Make your changes with clear commit messages
4. Submit a pull request

Please ensure your code follows the existing style and includes appropriate tests and documentation.

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.
