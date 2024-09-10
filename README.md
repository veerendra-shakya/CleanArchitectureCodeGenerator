# CleanArchitectureCodeGenerator

CleanArchitectureCodeGenerator is a .NET 8 console application designed to automate code scaffolding for the [CleanArchitectureWithBlazorServer](https://github.com/neozhu/CleanArchitectureWithBlazorServer) project by Neozhu. This tool helps streamline development by generating essential code components, ensuring adherence to best practices and architectural guidelines.

## What does this tool do?

This tool is designed to streamline the development of Clean Architecture applications. It automates the generation of essential code components, ensuring your project follows the best practices and architectural guidelines.

## Features:
- Generates boilerplate code for domain, application, infrastructure, and UI layers.
- Ensures separation of concerns with clear boundaries between layers.
- Supports customization to fit your specific project needs.
- Easy to integrate into your existing workflow.

## How to Use:
Simply run this tool and follow the on-screen prompts. You'll be guided through the process of generating code for each layer of your application, from entity classes to repository interfaces and beyond.

## Rules for Entity Creation:
1. **Naming**: Use Camel Case (e.g., "ProductCode") for both class and property names.
2. **Restrictions**: Avoid spaces, hyphens (`-`), and underscores (`_`).
3. **Singular Naming**: Entity names should be singular.
4. **Required**: `public string? Name { get; set; }` must be included for scaffolding.

### Inheritance Options:
Entities can inherit from:
- `BaseAuditableSoftDeleteEntity`
- `BaseAuditableEntity`
- `BaseEntity`
- `IEntity`
- `ISoftDelete`

### Supported Data Annotations:
- **Property Data Annotations**: `[Display(Name = "")]`, `[Description("")]`
- **Validation Data Annotations**: `[Required]`, `[MaxLength(100)]`, `[Range(0, 10)]`, `[RegularExpression(@"^[a-zA-Z0-9]*$", ErrorMessage = "Only alphanumeric characters are allowed.")]`

### Recommendations:
- For scaffolding, basic validation attributes on your entities are encouraged.
- For more complex scenarios, use a hybrid approach: basic validations in attributes and advanced logic in dedicated validators.

Two sample entities/models are available for convenience and can be generated in your domain project using commands 4 and 5.

## Credits:
This project was developed for use with the [CleanArchitectureWithBlazorServer](https://github.com/neozhu/CleanArchitectureWithBlazorServer) project. Full credit goes to Neozhu for the original Clean Architecture Blazor Server implementation.
