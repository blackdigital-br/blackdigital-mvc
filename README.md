# BlackDigital.AspNet

[🇧🇷 Português](README.pt.md) | 🇺🇸 English

<div align="center">
  <img src="https://raw.githubusercontent.com/blackdigital-br/blackdigital-mvc/main/doc/images/Logo128.png" alt="BlackDigital.AspNet Logo" width="128" height="128">
  
  **Modern framework for REST API development in .NET**
  
  [![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
  [![.NET](https://img.shields.io/badge/.NET-10.0%2B-purple.svg)](https://dotnet.microsoft.com/)
</div>

## 📋 About the Project

**BlackDigital.AspNet** is an innovative framework that dramatically simplifies REST API development in .NET, eliminating the need for traditional controllers and offering an advanced transformation system for API evolution with full compatibility.

### 🚀 Key Features

- **🔧 REST System**: Create APIs without controllers using service interfaces and attributes
- **🔄 Transformation System**: API evolution maintaining backward compatibility
- **📖 OpenAPI + Scalar**: Automatic API documentation generation (no Swashbuckle)
- **⚡ High Performance**: Optimized middleware for maximum efficiency
- **🧪 Testability**: Architecture that facilitates unit and integration testing
- **📦 Modular**: Independent and reusable components

### 💡 Why use BlackDigital.AspNet?

- **Productivity**: Reduce up to 70% of boilerplate code
- **Maintainability**: Cleaner and more organized code
- **Safe Evolution**: Update APIs without breaking existing clients
- **Flexibility**: Coexists perfectly with traditional controllers
- **Simplicity**: Focus on business logic, not infrastructure

## 📦 Installation

### Via NuGet Package Manager
```bash
Install-Package BlackDigital.AspNet
```

### Via .NET CLI
```bash
dotnet add package BlackDigital.AspNet
```

### Via PackageReference
```xml
<PackageReference Include="BlackDigital.AspNet" Version="25.10.7.16" />
```

## ⚙️ Basic Configuration

### 1. Configure in Program.cs

```csharp
using BlackDigital.AspNet.OpenApi;
using BlackDigital.AspNet.Rest;
using BlackDigital.AspNet.Rest.Transforms;

var builder = WebApplication.CreateBuilder(args);

// Register REST services (interface + implementation)
builder.Services.AddRestServices(rest =>
{
    rest.AddService<IUserService, UserService>();
});

// Configure MVC options (model binders, route constraints, validation filter)
builder.Services.AddRestMvcOptions();

// Configure the transformation system (optional)
builder.Services.AddTransform(transform =>
{
    transform.AddInputRule<OldUserToUserRule>("POST:api/users/", "2020-01-01");
});

// Configure OpenAPI + Scalar documentation (optional)
builder.Services.AddOpenApi(options =>
{
    options.Title = "My API";
    options.Version = "v1";
    options.Enabled = builder.Environment.IsDevelopment() || builder.Environment.IsStaging();
});

var app = builder.Build();

app.UseRouting();
app.UseRestMiddleware();   // REST middleware (before UseAuthorization)
app.UseAuthorization();
app.MapControllers();      // Traditional controllers keep working
app.MapOpenApi();          // OpenAPI JSON + Scalar UI

app.Run();
```

### 2. Create your first service

Services are defined as **interfaces** decorated with `[Service]`, with methods decorated with `[Action]`:

```csharp
using BlackDigital.Rest;

[Service("api/users")]
public interface IUserService
{
    [Action("{id}", method: RestMethod.Get)]
    Task<User> GetUserAsync([Path] int id);

    [Action(method: RestMethod.Post)]
    Task<User> CreateUserAsync([Body] CreateUserRequest request);
}
```

Implement the interface as a regular class (no controller base class required). Access
requirements are declared on the **implementation** with `[Authorize]` (from
`BlackDigital.Domain.Security`), never on the interface:

```csharp
using BlackDigital.Domain.Security;

public class UserService : IUserService
{
    public async Task<User> GetUserAsync(int id)
    {
        // Your logic here
        return await GetUserByIdAsync(id);
    }

    [Authorize]   // any authenticated user; narrow with [Authorize(Roles = ...)] or [Authorize(Policy = ...)]
    public async Task<User> CreateUserAsync(CreateUserRequest request)
    {
        // Your logic here
        return await CreateUserAsync(request);
    }
}
```

## 🎯 Main Features

### 🔧 REST System
Create complete REST APIs without traditional controllers:
- **Interface-based**: `[Service]` and `[Action]` attributes define routes automatically
- **Automatic Binding**: `[Path]`, `[Body]`, `[Query]`, `[Header]` parameters bound automatically
- **Built-in `Id` support**: Custom model binder and route constraint for `BlackDigital.Model.Id`
- **CancellationToken**: Request abortion token is bound automatically
- **Authorization**: `[Authorize]` (roles/policy) declared on the service implementation
- **Business exceptions**: `BusinessException` maps directly to HTTP status codes
- **Full Support**: GET, POST, PUT, DELETE, PATCH, HEAD, OPTIONS
- **Async/Await**: Native support for `Task`, `Task<T>`, `ValueTask`, `ValueTask<T>`

### 🔄 Transformation System
Evolve your APIs maintaining compatibility:
- **Input and Output rules**: Transform request body and response independently
- **Date-based versioning**: Version selected via `api-version` query string or `x-api-version` header
- **Automatic chaining**: Rules are applied sequentially by version order
- **DI integration**: Rule types are instantiated through the DI container

### 📖 OpenAPI + Scalar
Automatic, Swashbuckle-free documentation:
- **Pure Microsoft.OpenApi**: Built directly with `Microsoft.OpenApi` 3.x
- **Automatic discovery**: Scans assemblies for `[Service]` interfaces
- **Scalar UI**: Interactive API documentation via `Scalar.AspNetCore`
- **Bearer JWT**: Pre-configured security scheme (overridable)
- **Customizable**: Title, version, contact, license, server URL, assembly filters

> **Note:** Security (JWT, password hashing) and Email infrastructure are now part of the [BlackDigital.Domain](https://github.com/blackdigital-br/blackdigital-net-domain) package.

## 📚 Detailed Documentation

### 📖 Complete Guides
- **[REST System](doc/rest-system.md)** - Complete REST system guide
- **[Transformation System](doc/transform-system.md)** - Complete transformation system guide
- **[OpenAPI + Scalar](doc/openapi.md)** - API documentation generation guide

### 🎯 Quick Navigation
- [How to create REST services](doc/rest-system.md#how-to-use-practical-implementation)
- [Transformation configuration](doc/transform-system.md#configuration-and-usage)
- [OpenAPI configuration](doc/openapi.md#configuration)
- [Best practices](doc/rest-system.md#best-practices)

## 🚀 Quick Examples

### Simple REST Service
```csharp
[Service("api/products")]
public interface IProductService
{
    [Action(method: RestMethod.Get)]
    Task<IEnumerable<Product>> GetProductsAsync();

    [Action("{id}", method: RestMethod.Get)]
    Task<Product> GetProductAsync([Path] int id);

    [Action(method: RestMethod.Post)]
    Task<Product> CreateProductAsync([Body] Product product);
}
```

### Basic Transformation
```csharp
public class OldUserToUserRule : TransformRule<OldUser, User>
{
    public override User? Transform(OldUser? source)
    {
        if (source == null) return null;
        return new User
        {
            Id = source.Id,
            FullName = $"{source.FirstName} {source.LastName}",
            Email = source.Email
        };
    }
}
```

### 📁 Complete Example Project
Explore the [example project](example/) that demonstrates:
- Complete configuration
- REST services (interface-based)
- Input and output transformations
- OpenAPI + Scalar setup

## 📁 Project Structure

```
BlackDigital.AspNet/
├── 📁 src/                    # Main source code
│   ├── 📁 Rest/              # REST system (middleware, routing, binding)
│   │   └── 📁 Transforms/    # Transformation engine
│   ├── 📁 OpenApi/           # OpenAPI document builder + Scalar integration
│   ├── 📁 Binder/            # Custom model binders (Id)
│   ├── 📁 Constraint/        # Route constraints (id)
│   ├── 📁 Filters/           # MVC filters (ModelState validation)
│   └── 📁 Services/          # DI helpers and HTTP context utilities
├── 📁 example/               # Example project
├── 📁 test/                  # Unit tests
├── 📁 doc/                   # Documentation
│   ├── 📄 rest-system.md     # REST system documentation
│   ├── 📄 transform-system.md # Transformation documentation
│   ├── 📄 openapi.md         # OpenAPI + Scalar documentation
│   └── 📁 images/            # Documentation images
└── 📄 README.md              # This file
```

### 🔍 Where to Find
- **Examples**: `example/` folder contains complete implementation
- **Tests**: `test/` folder with usage examples and tests
- **Documentation**: `doc/` folder with detailed guides
- **Source Code**: `src/` folder with implementation

## 🤝 Contributing

Contributions are welcome! To contribute:

1. **Fork** the project
2. **Create** a branch for your feature (`git checkout -b feature/AmazingFeature`)
3. **Commit** your changes (`git commit -m 'Add some AmazingFeature'`)
4. **Push** to the branch (`git push origin feature/AmazingFeature`)
5. **Open** a Pull Request

### 📋 Guidelines
- Follow existing code standards
- Add tests for new features
- Update documentation when necessary
- Use English for code and comments

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🆘 Support

### 📞 Support Channels
- **Issues**: [GitHub Issues](https://github.com/blackdigital-br/blackdigital-mvc/issues)
- **Discussions**: [GitHub Discussions](https://github.com/blackdigital-br/blackdigital-mvc/discussions)
- **Email**: support@blackdigital.com.br

### 📚 Additional Resources
- [Complete Documentation](doc/)
- [Practical Examples](example/)

---

<div align="center">
  <p>Made with ❤️ by the <strong>BlackDigital</strong> team</p>
  <p>
    <a href="https://blackdigital.com.br">Website</a> •
    <a href="doc/">Documentation</a> •
    <a href="example/">Examples</a> •
    <a href="https://github.com/blackdigital-br/blackdigital-mvc/issues">Support</a>
  </p>
</div>
