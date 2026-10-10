# BlackDigital.AspNet

[🇧🇷 Português](README.pt.md) | 🇺🇸 English

<div align="center">
  <img src="doc/images/Logo128.png" alt="BlackDigital.AspNet Logo" width="128" height="128">
  
  **Modern framework for REST API development in .NET**
  
  [![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
  [![.NET](https://img.shields.io/badge/.NET-10.0%2B-purple.svg)](https://dotnet.microsoft.com/)
  
  **[🇧🇷 Português](README.pt.md) | 🇺🇸 English**
</div>

## 📋 About the Project

**BlackDigital.AspNet** is an innovative framework that dramatically simplifies REST API development in .NET, eliminating the need for traditional controllers and offering an advanced transformation system for API evolution with full compatibility.

### 🚀 Key Features

- **🔧 REST System**: Create APIs without controllers using only attributes
- **🔄 Transformation System**: API evolution maintaining backward compatibility
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
using BlackDigital.AspNet;

var builder = WebApplication.CreateBuilder(args);

// Add BlackDigital.AspNet services
builder.Services.AddBlackDigitalServices();

var app = builder.Build();

// Configure REST middleware
app.UseBlackDigitalRest();

// Configure transformation system (optional)
app.UseBlackDigitalTransforms();

app.Run();
```

### 2. Create your first service

```csharp
[Service("api/users")]
public class UserService
{
    [Action("GET")]
    public async Task<IEnumerable<User>> GetUsers()
    {
        // Your logic here
        return await GetAllUsersAsync();
    }

    [Action("POST")]
    public async Task<User> CreateUser([FromBody] CreateUserRequest request)
    {
        // Your logic here
        return await CreateUserAsync(request);
    }
}
```

## 🎯 Main Features

### 🔧 REST System
Create complete REST APIs without traditional controllers:
- **Simple Attributes**: `[Service]` and `[Action]` define routes automatically
- **Automatic Binding**: Parameters bound automatically
- **Full Support**: GET, POST, PUT, DELETE, PATCH
- **Async/Await**: Native support for asynchronous operations

### 🔄 Transformation System
Evolve your APIs maintaining compatibility:
- **Automatic Versioning**: Transparent transformations between versions
- **Full Compatibility**: Old clients continue working
- **Flexible Rules**: Define custom transformations
- **Performance**: Zero overhead for current versions

### 🔗 Perfect Integration
Systems work together:
- **REST + Transformation**: APIs that evolve without breaking
- **Unified Configuration**: Simple and consistent setup
- **Monitoring**: Integrated logs and metrics

## 📚 Detailed Documentation

### 📖 Complete Guides
- **[REST System](doc/rest-system.md)** - Complete REST system guide
- **[Transformation System](doc/transform-system.md)** - Complete transformation system guide

### 🎯 Quick Navigation
- [How to create REST services](doc/rest-system.md#how-to-use)
- [Transformation configuration](doc/transform-system.md#configuration-and-usage)
- [Practical examples](doc/rest-system.md#practical-examples)
- [Best practices](doc/rest-system.md#best-practices)

## 🚀 Quick Examples

### Simple REST Service
```csharp
[Service("api/products")]
public class ProductService
{
    [Action("GET")]
    public async Task<IEnumerable<Product>> GetProducts()
        => await _repository.GetAllAsync();

    [Action("GET", "{id}")]
    public async Task<Product> GetProduct(int id)
        => await _repository.GetByIdAsync(id);

    [Action("POST")]
    public async Task<Product> CreateProduct([FromBody] Product product)
        => await _repository.CreateAsync(product);
}
```

### Basic Transformation
```csharp
public class UserTransformRule : ITransformRule<OldUser, NewUser>
{
    public NewUser Transform(OldUser source)
    {
        return new NewUser
        {
            Id = source.Id,
            FullName = $"{source.FirstName} {source.LastName}",
            Email = source.Email,
            CreatedAt = source.Created
        };
    }
}
```

### 📁 Complete Example Project
Explore the [example project](example/) that demonstrates:
- Complete configuration
- REST services
- Transformations
- Entity Framework integration
- Unit tests

## 📁 Project Structure

```
BlackDigital.AspNet/
├── 📁 src/                    # Main source code
│   ├── 📁 Rest/              # REST system
│   ├── 📁 Binder/            # Custom model binders
│   ├── 📁 Constraint/        # Route constraints
│   ├── 📁 Services/          # Auxiliary services
│   └── 📁 Infrastructures/   # Infrastructure
├── 📁 example/               # Example project
├── 📁 test/                  # Unit tests
├── 📁 doc/                   # Documentation
│   ├── 📄 rest-system.md     # REST system documentation
│   ├── 📄 transform-system.md # Transformation documentation
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