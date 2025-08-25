# Niobium.Channel.Blazor.Fluent

Niobium.Channel.Blazor.Fluent is a Blazor WebAssembly UI framework for rapid, professional, and consistent business application development, built on top of the Niobium framework. It provides reusable, extensible, and mobile-first UI components using Microsoft Fluent UI for Blazor, enabling fast implementation of CRUD, forms, and data-driven experiences.

## What is this project about?
- Provides advanced, property-driven UI components for Blazor (e.g., `StackEditView`, `CardDisplayView`).
- Ensures a consistent, modern, and mobile-first look and feel for business applications.
- Integrates deeply with Niobium's domain-driven and event-driven architecture.
- Used by consumer projects to quickly build forms, lists, and data entry UIs.

## Getting Started

### 1. Install the NuGet Package
Add the package to your Blazor WebAssembly project:
dotnet add package Niobium.Channel.Blazor.Fluent
### 2. Register Services in Program.cs
Add the Fluent UI and Niobium services to your DI container:
using Microsoft.FluentUI.AspNetCore.Components;
using Niobium.Channel.Blazor.Fluent;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.Services
    .AddFluentUIComponents()
    .AddChannelBlazorFluent();
### 3. Use Components in Your Blazor Pages
Import the namespace in your `_Imports.razor`:
@using Niobium.Channel.Blazor.Fluent
Example usage in a Razor component:
<StackEditView Data="@myViewModel" />
<CardDisplayView Data="@myListViewModel" />
- `StackEditView` renders a responsive, property-driven edit form for a single view model.
- `CardDisplayView` renders a responsive, card-based list for a collection of view models.

### 4. Entry Point & Dependency Injection
To enable Niobium framework integration, ensure you register the Niobium services in your DI setup. For example:
builder.Services.AddChannelBlazor();
## Contributing

Contributions are welcome! To contribute:
1. Fork the repository
2. Create a feature branch
3. Make your changes with clear commit messages
4. Submit a pull request

Please ensure your code follows the existing style and includes appropriate tests and documentation.

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.
