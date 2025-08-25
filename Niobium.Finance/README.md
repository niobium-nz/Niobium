# Niobium.Finance

Niobium.Finance provides core abstractions and types for financial transactions, payment processing, and accounting in the Niobium ecosystem. It defines contracts for transaction requests, charge responses, and payment methods for business applications.

## What is this project about?
- Defines types for transaction requests, charge responses, and payment method kinds.
- Used as a foundational dependency by Niobium.Channel and other Niobium.* packages for financial operations.
- Enables extensible, type-safe financial and payment logic.

## Getting Started

### 1. Install the NuGet Package
Add the package to your .NET project:

```
dotnet add package Niobium.Finance
```

### 2. Use the Types in Your Code
Use the provided types for financial operations in your domain and infrastructure code:

```csharp
using Niobium.Finance;

var request = new TransactionRequest
{
    Target = "user-123",
    Delta = 1000,
    Reason = 1,
    Remark = "Initial deposit"
};
```

### 3. Integrate with Other Niobium Packages
Niobium.Finance is automatically referenced by Niobium.Channel and other Niobium.* packages. Just add the relevant Niobium.* packages to your project and use the shared contracts.

## Contributing

Contributions are welcome! To contribute:
1. Fork the repository
2. Create a feature branch
3. Make your changes with clear commit messages
4. Submit a pull request

Please ensure your code follows the existing style and includes appropriate tests and documentation.

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.
