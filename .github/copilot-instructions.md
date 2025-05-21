# Code Style & Formatting
We use C# 12 file-scoped namespaces.  
Use four-space indentation in all Razor components.  
CamelCase for private fields; PascalCase for public properties.  
Insert trailing commas in multi-line initializer lists.  
Respect `.editorconfig` settings for Blazor formatting.  
Prefer explicit types for built-in types, `var` elsewhere.  
Use trailing commas in all multi-line initializers and object/collection initializations.  
Do not use regions or pragmas in code.  

# Project & Framework Context
This is a .NET 9 Blazor Aspire.NET solution with multiple projects.  
Place UI pages under `Pages/`, shared components under `Shared/`.  
Keep project structure flat.  
Use Blazor FluentUI for dialogs, forms, and layouts.  
Register services via `builder.Services` in `Program.cs`.  
Follow top-level statements and minimal host startup.  
Use dependency injection for all services; never instantiate services directly.  
Use `Program.cs` for all service registrations and configuration.  

# Testing & Validation
Don't add any tests.  

# Architecture & Design
Apply MVU (Model-View-Update) in Blazor component design.  
Depend on interfaces injected via constructor only.  
Use `EventCallback` and `EventCallback<T>` for parent-child communication in components.  
Use deep copies for editing models in dialogs to avoid mutating parent state until confirmed.  
Use Javascript interop for all JavaScript calls.
Disable TrapFocus in FluentUI DOM on all open dialogs via Javascript dynamically when additional nested dialogs are shown.
Use `Guid` for entity IDs and `DateTimeOffset` for all date/time values.  
Use `StateHasChanged()` only when necessary to trigger UI updates.  

# Documentation & Comments
Add XML `<summary>` to all public methods, properties, and components.  
Annotate Razor event callbacks with `<param>` tags.  
Keep comments concise and focused on "why, not "what."  

# Logging & Error Handling
Use `ILogger<T>` for logging in all services and components; prefer `LogInformation` for user actions, `LogWarning` for recoverable issues.  
Use `toastService.ShowError/ShowSuccess/ShowInfo/ShowWarning` for user feedback in Blazor UI.  
Throw `AspireException` for domain errors with clear, localized messages.  
Wrap host build in try/catch to log startup failures.  
Always use `async`/`await` for asynchronous operations in Blazor components and services.  

# Security & Compliance
Validate inputs with FluentValidation in `AzUrlShortener.AdminUI`.  
Validate all user input, especially URLs and cron expressions, using both regex and FluentValidation.  
Use `[Authorize]` and `[ValidateAntiForgeryToken]` attributes on all sensitive or form-handling Razor components.  
Escape all user-supplied strings in Razor output to prevent XSS.  
Enforce HTTPS redirection and HSTS in production.  

# UI & Dialogs
Use `DialogService.ShowDialogAsync<TComponent>` for modal dialogs; always handle dialog results and cancellation.  
Use Blazor FluentUI for all UI elements, dialogs, and forms.  
