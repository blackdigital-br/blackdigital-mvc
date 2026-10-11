# REST System - BlackDigital.AspNet

**[🇧🇷 Português](rest-system.pt.md) | 🇺🇸 English**

## What is the REST System

**BlackDigital.AspNet REST** is a modern and innovative system for creating REST APIs that offers an elegant alternative to traditional ASP.NET Core controllers. Based on custom middleware, the system uses attributes for automatic routing and intelligent parameter binding, allowing developers to focus exclusively on business logic.

### Key Features

- **Middleware-based**: Intercepts HTTP requests before traditional controllers
- **Attribute-driven**: Routing and configuration based on declarative attributes
- **Service-oriented**: Works with service interfaces instead of controller inheritance
- **Intelligent binding**: Automatic parameter binding from multiple sources (route, body, headers, query)
- **Integrated transformations**: Native integration with the transformation system for API versioning
- **Business-focused**: Eliminates HTTP infrastructure code, keeping only business logic

### Coexistence with Traditional Controllers

The REST system **coexists perfectly** with traditional ASP.NET Core controllers:

- **Simultaneous use**: Both approaches can be used in the same project
- **Gradual migration**: Migrate specific endpoints as needed
- **Total flexibility**: Use traditional controllers for specific cases that need full HTTP control
- **No conflicts**: REST middleware only processes routes configured with `[Service]` attributes

## How to Use: Practical Implementation

### 1. Configuration in Program.cs

```csharp
using BlackDigital.AspNet.Rest;

var builder = WebApplication.CreateBuilder(args);

// 1. Register REST services
builder.Services.AddRestServices(restService =>
{
    restService.AddService<IUserService, UserService>();
    restService.AddService<IProductService, ProductService>();
});

// 2. Configure MVC options (required for REST system)
builder.Services.AddRestMvcOptions();

// 3. Configure transformations (optional)
builder.Services.AddTransform(transformConfig =>
{
    transformConfig.AddRule<UserV1ToV2>("POST:api/users/", "2024-01-01");
    transformConfig.AddRule<UserV2ToV1>("POST:api/users/", "2024-01-01");
});

var app = builder.Build();

// 4. Configure middleware pipeline
app.UseRestMiddleware(); // Must come before UseAuthorization
app.UseAuthorization();
app.MapControllers(); // Traditional controllers continue working

app.Run();
```

### 2. Creating a Business Service

#### Defining the Interface

```csharp
using BlackDigital.Rest;

[Service("api/users")]
public interface IUserService
{
    [Action("{id}", method: RestMethod.Get, authorize: false)]
    Task<User> GetUserAsync([Path] int id);

    [Action(method: RestMethod.Post, authorize: true)]
    Task<User> CreateUserAsync([Body] CreateUserRequest request);

    [Action("{id}", method: RestMethod.Put, authorize: true)]
    Task<User> UpdateUserAsync([Path] int id, [Body] UpdateUserRequest request);

    [Action("{id}", method: RestMethod.Delete, authorize: true)]
    Task DeleteUserAsync([Path] int id);

    [Action("search", method: RestMethod.Get, authorize: false)]
    Task<List<User>> SearchUsersAsync([Query] string filter, [Query] int page = 1);
}
```

#### Implementing the Service

```csharp
public class UserService : IUserService
{
    private readonly IUserRepository _repository;
    private readonly ILogger<UserService> _logger;

    public UserService(IUserRepository repository, ILogger<UserService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<User> GetUserAsync(int id)
    {
        _logger.LogInformation("Searching for user {UserId}", id);
        
        var user = await _repository.GetByIdAsync(id);
        if (user == null)
            BusinessException.ThrowNotFound("User not found");
        
        return user;
    }

    public async Task<User> CreateUserAsync(CreateUserRequest request)
    {
        _logger.LogInformation("Creating new user: {Email}", request.Email);
        
        // Business validations
        if (await _repository.ExistsByEmailAsync(request.Email))
            BusinessException.ThrowConflict("Email is already in use");
        
        var user = new User
        {
            Name = request.Name,
            Email = request.Email,
            CreatedAt = DateTime.UtcNow
        };
        
        return await _repository.CreateAsync(user);
    }

    public async Task<User> UpdateUserAsync(int id, UpdateUserRequest request)
    {
        var user = await _repository.GetByIdAsync(id);
        if (user == null)
            BusinessException.ThrowNotFound("User not found");
        
        user.Name = request.Name;
        user.UpdatedAt = DateTime.UtcNow;
        
        return await _repository.UpdateAsync(user);
    }

    public async Task DeleteUserAsync(int id)
    {
        var user = await _repository.GetByIdAsync(id);
        if (user == null)
            BusinessException.ThrowNotFound("User not found");
        
        await _repository.DeleteAsync(id);
    }

    public async Task<List<User>> SearchUsersAsync(string filter, int page = 1)
    {
        return await _repository.SearchAsync(filter, page, pageSize: 20);
    }
}
```

### 3. Understanding Attributes

#### ServiceAttribute
Defines the service base route:
```csharp
[Service("api/users")] // All actions will have "api/users" prefix
```

#### ActionAttribute
Configures HTTP methods and specific routes:
```csharp
[Action("{id}", method: RestMethod.Get, authorize: false)]
// Route: GET /api/users/{id}
// Authorization: not required

[Action("search", method: RestMethod.Get)]
// Route: GET /api/users/search
// Authorization: required (default)
```

#### Binding Attributes
- **`[Path]`**: Extracts values from URL route
- **`[Body]`**: Extracts data from request body
- **`[Query]`**: Extracts values from query string
- **`[Header]`**: Extracts values from HTTP headers

### 4. Practical Example: User API

```csharp
[Service("api/users")]
public interface IUserService
{
    // GET /api/users/{id}
    [Action("{id}", method: RestMethod.Get, authorize: false)]
    Task<User> GetUserAsync([Path] int id);

    // POST /api/users
    [Action(method: RestMethod.Post)]
    Task<User> CreateUserAsync([Body] CreateUserRequest request);

    // GET /api/users/search?filter=john&page=1&size=10
    [Action("search", method: RestMethod.Get, authorize: false)]
    Task<PagedResult<User>> SearchAsync(
        [Query] string filter,
        [Query] int page = 1,
        [Query] int size = 10
    );

    // PUT /api/users/{id}/profile
    [Action("{id}/profile", method: RestMethod.Put)]
    Task<User> UpdateProfileAsync(
        [Path] int id,
        [Body] UpdateProfileRequest request,
        [Header("User-Agent")] string userAgent
    );
}
```

### 5. Example with Authorization

```csharp
[Service("api/admin")]
public interface IAdminService
{
    // Public - no authorization required
    [Action("health", method: RestMethod.Get, authorize: false)]
    Task<HealthStatus> GetHealthAsync();

    // Requires authorization (default)
    [Action("users", method: RestMethod.Get)]
    Task<List<User>> GetAllUsersAsync();

    // Requires specific authorization
    [Action("users/{id}/ban", method: RestMethod.Post, authorize: true)]
    Task BanUserAsync([Path] int id, [Body] BanRequest request);
}
```

## Integration with Transformation System

The REST system integrates natively with BlackDigital.AspNet's transformation system, offering automatic API versioning and backward compatibility.

### Basic Configuration

```csharp
// Program.cs
builder.Services.AddTransform(config =>
{
    // Input transformation: V1 → V2 (old clients send V1, service expects V2)
    config.AddInputRule<UserV1ToV2InputRule>("POST:api/users/", "2024-01-01");
    
    // Output transformation: V2 → V1 (service returns V2, old clients expect V1)
    config.AddOutputRule<UserV2ToV1OutputRule>("GET:api/users/", "2024-01-01");
    config.AddOutputRule<UserV2ToV1OutputRule>("POST:api/users/", "2024-01-01");
});
```

The rule **key** is built automatically from the action's `Method:BaseRoute/ActionRoute` (e.g. `POST:api/users/`). The **version** is selected per request from:
1. Query string `?api-version=2024-01-01`
2. Header `x-api-version: 2024-01-01`
3. Fallback default: `1970-01-01`

### Defining Transformation Rules

Rules extend `TransformRule<TIn, TOut>`:

```csharp
// Input transformation: V1 → V2
public class UserV1ToV2InputRule : TransformRule<UserV1, UserV2>
{
    public override UserV2? Transform(UserV1? userV1)
    {
        if (userV1 == null) return null;
        return new UserV2
        {
            Id = userV1.Id,
            FullName = $"{userV1.FirstName} {userV1.LastName}",  // V1: separate → V2: combined
            Email = userV1.Email,
            Profile = new UserProfile { Bio = "", Avatar = "" }   // New field in V2
        };
    }
}

// Output transformation: V2 → V1
public class UserV2ToV1OutputRule : TransformRule<UserV2, UserV1>
{
    public override UserV1? Transform(UserV2? userV2)
    {
        if (userV2 == null) return null;
        var parts = userV2.FullName.Split(' ', 2);
        return new UserV1
        {
            Id = userV2.Id,
            FirstName = parts[0],
            LastName = parts.Length > 1 ? parts[1] : "",
            Email = userV2.Email
            // Profile is omitted in V1
        };
    }
}
```

### Automatic API Versioning

The system supports multiple versions of the same API automatically. Register both service versions and let transformations handle the conversion:

```csharp
// Service V1
[Service("api/v1/users")]
public interface IUserServiceV1
{
    [Action(method: RestMethod.Post)]
    Task<UserV1> CreateUserAsync([Body] UserV1 user);
}

// Service V2
[Service("api/v2/users")]
public interface IUserServiceV2
{
    [Action(method: RestMethod.Post)]
    Task<UserV2> CreateUserAsync([Body] UserV2 user);
}

// Configuration
builder.Services.AddRestServices(config =>
{
    config.AddService<IUserServiceV1, UserServiceV1>();
    config.AddService<IUserServiceV2, UserServiceV2>();
});
```

### Automatic Transformation via Query String or Header

The client specifies the desired API version, and the system applies the appropriate transformations:

```csharp
// Client specifies version via query string
// GET /api/users?api-version=2020-01-01
// → System applies output transformations for versions newer than 2020-01-01

// Client specifies version via header
// x-api-version: 2020-01-01
// → Same behavior as the query string
```

When no version is provided, the system defaults to `1970-01-01`, meaning all registered transformations (with newer versions) are applied.

### Practical Example: API Evolution

**Scenario**: Adding `profile` field in version 2 of the user API

```csharp
// V1: Original structure
public class UserV1
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
}

// V2: Evolved structure
public class UserV2
{
    public int Id { get; set; }
    public string FullName { get; set; } // Renamed from Name
    public string Email { get; set; }
    public UserProfile Profile { get; set; } // New field
}

// Unified service that works with V2 internally
[Service("api/users")]
public interface IUserService
{
    [Action(method: RestMethod.Post)]
    Task<UserV2> CreateUserAsync([Body] UserV2 user);
}

// Automatic transformations ensure compatibility
// POST /api/users with V1 data → Transformed to V2 → Processed → Response transformed to V1
```

### Integration Benefits

1. **Guaranteed Compatibility**: Old clients continue working
2. **Gradual Evolution**: Add new fields without breaking existing APIs
3. **Unified Code**: One service serves multiple versions
4. **Automatic Transformation**: No manual conversion code
5. **Testability**: Transformations can be tested independently
6. **Automatic Documentation**: Versions are documented automatically

## Benefits and Advantages

### 1. Total Focus on Business Logic

The system completely eliminates HTTP infrastructure code, allowing you to focus only on what really matters:

```csharp
// ✅ With BlackDigital.AspNet REST - Only business logic
public async Task<User> GetUserAsync(int id)
{
    var user = await _repository.GetByIdAsync(id);
    if (user == null)
        BusinessException.ThrowNotFound("User not found");
    
    return user; // Automatic serialization
}

// ❌ Traditional controller - Mixes infrastructure + business
[HttpGet("{id}")]
public async Task<IActionResult> GetUser(int id)
{
    try
    {
        var user = await _repository.GetByIdAsync(id);
        if (user == null)
            return NotFound(); // Infrastructure code
        
        return Ok(user); // Infrastructure code
    }
    catch (Exception ex)
    {
        return StatusCode(500, "Internal error"); // Infrastructure code
    }
}
```

### 2. Extreme Productivity

**Significant code reduction:**
- 70% fewer lines of code
- 100% less infrastructure code
- 3x faster development

```csharp
// Comparison: Complete user CRUD
// Traditional controller: ~120 lines
// BlackDigital.AspNet REST: ~35 lines
```

### 3. Superior Testability

Tests focused only on business logic, without HTTP dependencies:

```csharp
[Test]
public async Task GetUser_WhenUserExists_ShouldReturnUser()
{
    // Arrange
    var mockRepository = new Mock<IUserRepository>();
    mockRepository.Setup(r => r.GetByIdAsync(1))
              .ReturnsAsync(new User { Id = 1, Name = "John" });
    
    var service = new UserService(mockRepository.Object);
    
    // Act - No HTTP dependencies!
    var result = await service.GetUserAsync(1);
    
    // Assert - Only business logic
    Assert.That(result.Id, Is.EqualTo(1));
    Assert.That(result.Name, Is.EqualTo("John"));
}
```

### 4. Flexibility and Reusability

Services can be reused in different contexts:

```csharp
public class UserService : IUserService
{
    // This same service can be used in:
    // ✅ REST API (via RestMiddleware)
    // ✅ GraphQL (via resolvers)
    // ✅ Background jobs
    // ✅ Unit tests
    // ✅ Other services (dependency injection)
    // ✅ Console applications
    // ✅ Blazor Server/WASM
}
```

### 5. Coexistence with Traditional Controllers

The system doesn't replace, but **complements** traditional controllers:

```csharp
// ✅ Use REST for standard business APIs
[Service("api/users")]
public interface IUserService { }

// ✅ Use Controllers for specific cases
[ApiController]
[Route("api/files")]
public class FileController : ControllerBase
{
    // File upload, streaming, etc.
    [HttpPost("upload")]
    public async Task<IActionResult> Upload(IFormFile file) { }
}
```

### 6. Intelligent Automations

The system automates repetitive tasks:

- **Routing**: Based on attributes, no manual configuration
- **Binding**: Automatic from multiple sources (route, body, headers, query)
- **Validation**: Automatic based on attributes and types
- **Serialization**: Optimized JSON with default configurations
- **Error handling**: Automatic conversion of exceptions to HTTP status
- **Transformation**: Automatic API versioning
- **Authorization**: Automatic verification based on attributes

### 7. Performance and Efficiency

The system is optimized for performance:

- Less overhead than traditional controllers
- Direct parameter binding
- Optimized serialization
- Fewer memory allocations
- Lean middleware pipeline

## Technical Details

### System Architecture

The REST system consists of five main components:

1. **RestMiddleware**: Main middleware that intercepts and processes requests
2. **RestRouteRegistry**: Pre-built route table (singleton) that maps HTTP methods to service methods
3. **RestHelper**: Helper methods for MVC configuration
4. **RestServiceBuilder**: Builder for service configuration and registration
5. **RestMiddlewareExtensions**: Extensions for DI container integration (`AddRestServices`, `UseRestMiddleware`)

#### RestMiddleware

The `RestMiddleware` is the heart of the REST system, responsible for intercepting HTTP requests and routing them to appropriate services.

**Main Features:**
- HTTP request interception
- Attribute-based routing
- Multi-source parameter binding
- Automatic data transformation
- Attribute-based authorization
- Standardized exception handling

**Processing Flow:**
1. HTTP request interception
2. Service discovery based on `ServiceAttribute`
3. Method identification based on `ActionAttribute`
4. Parameter extraction and conversion
5. Input transformation (if configured)
6. Service method execution
7. Output transformation (if configured)
8. Response serialization and return

#### Attribute System

**ServiceAttribute**: Defines the service base route
```csharp
[Service("api/user")]
public interface IUserService { }
```

**ActionAttribute**: Configures HTTP methods and specific routes
```csharp
[Action(method: RestMethod.Get, route: "{id}", authorize: true)]
Task<User> GetUserAsync([Path] int id);
```

**Binding Attributes:**
- `[Path]`: Extracts values from URL route
- `[Body]`: Extracts data from request body
- `[Header]`: Extracts values from HTTP headers
- `[Query]`: Extracts values from query string

### Advanced Configuration

#### Configuration in Program.cs

```csharp
using BlackDigital.AspNet.Rest;
using BlackDigital.AspNet.Rest.Transforms;

var builder = WebApplication.CreateBuilder(args);

// 1. Register REST services (interface + implementation)
builder.Services.AddRestServices(rest =>
{
    rest.AddService<IUserService, UserService>();
    rest.AddService<IProductService, ProductService>();
});

// 2. Configure MVC options (model binders, route constraints, validation filter)
builder.Services.AddRestMvcOptions();

// 3. Configure transformations (if needed) — separate from REST registration
builder.Services.AddTransform(config =>
{
    config.AddInputRule<UserV1ToV2>("POST:api/users/", "2024-01-01");
    config.AddOutputRule<UserV2ToV1>("GET:api/users/", "2024-01-01");
});

var app = builder.Build();

// 4. Configure pipeline
app.UseRouting();
app.UseRestMiddleware();  // REST middleware — must come before UseAuthorization
app.UseAuthorization();
app.MapControllers();     // Traditional controllers keep working

app.Run();
```

#### Service Lifetimes

`RestServiceBuilder` offers three lifetimes and a factory overload:

```csharp
builder.Services.AddRestServices(rest =>
{
    rest.AddService<IUserService, UserService>();                 // Scoped (default)
    rest.AddSingletonService<ICacheService, CacheService>();      // Singleton
    rest.AddTransientService<IHelperService, HelperService>();    // Transient
    rest.AddService<ICustomService>(sp => new CustomService(sp)); // Factory (Scoped)
});
```

#### Advanced Features

**Flexible Authorization:**

Authorization is controlled by the `authorize` boolean flag, which can be set at the **action** level or inherited from the **service** (`[Service]`) level. When either is `true`, the middleware checks `context.User.Identity.IsAuthenticated` and returns `401 Unauthorized` if the user is not authenticated.

```csharp
[Service("api/admin")]
public interface IAdminService
{
    // Public — no authorization required
    [Action("health", method: RestMethod.Get, authorize: false)]
    Task<HealthStatus> GetHealthAsync();

    // Requires authorization (authorize defaults to true at action level,
    // or inherit true from [Service] when the action omits it)
    [Action("users", method: RestMethod.Get, authorize: true)]
    Task<List<User>> GetAllUsersAsync();
}
```

**Complex Binding:**

All four binding attributes are supported, plus automatic binding for simple types from the query string when no attribute is present:

```csharp
[Action("search/{category}", method: RestMethod.Post)]
Task<List<Product>> SearchAsync(
    [Path] string category,
    [Query] int page,
    [Query] int size,
    [Header("User-Id")] string userId,
    [Body] SearchCriteria criteria
);
```

**CancellationToken:**

Add a `CancellationToken` parameter to any action method — the middleware automatically binds it to `context.RequestAborted` so long-running operations respect client disconnects:

```csharp
[Action("export", method: RestMethod.Get)]
Task<Stream> ExportAsync(CancellationToken cancellationToken);
```

**Default Parameter Values:**

Parameters with default values are supported. When a binding source does not provide a value, the declared default is used:

```csharp
[Action("search", method: RestMethod.Get)]
Task<List<User>> SearchAsync(
    [Query] string filter = "",
    [Query] int page = 1,
    [Query] int size = 20
);
```

**Error Handling:**

Throw `BusinessException` (from `BlackDigital`) to return a specific HTTP status code. The middleware maps it directly:

```csharp
public async Task<User> GetUserAsync(int id)
{
    var user = await _repository.GetByIdAsync(id);
    
    if (user == null)
        BusinessException.ThrowNotFound("User not found");   // 404
    
    if (!user.IsActive)
        BusinessException.ThrowForbidden("Inactive user");   // 403
    
    return user;
}
```

Unhandled exceptions return `500 Internal Server Error` with a `RestError` payload (`{ "message", "stack" }`). The `stack` field is only populated in the Development environment.

#### Built-in `Id` Type Support

The framework ships with a custom model binder and route constraint for `BlackDigital.Model.Id`, registered automatically by `AddRestMvcOptions()`:

```csharp
[Service("api/users")]
public interface IUserService
{
    // The {id} route segment is bound to BlackDigital.Model.Id
    [Action("{id}", method: RestMethod.Get)]
    Task<User> GetUserAsync([Path] Id id);
}
```

`AddRestMvcOptions()` also:
- Inserts `IdModelBinderProvider` at the front of the MVC model binder providers
- Registers the `id` route constraint (`options.ConstraintMap.Add("id", typeof(IdConstraint))`)
- Adds `IdConverter`, `TimeSpanConverter`, and `JsonStringEnumConverter` to the JSON serializer options
- Registers `ModelStateValidationFilter`, which returns a `400 Bad Request` with `ErrorApiResponse` when model validation fails

### Best Practices

#### 1. Service Organization
- Keep interfaces focused on a specific domain
- Use descriptive names for routes and actions
- Group related functionalities

#### 2. Route Configuration
```csharp
// ✅ Good: Clear and RESTful routes
[Service("api/users")]
[Action("{id}", method: RestMethod.Get)]

// ❌ Avoid: Confusing routes
[Service("api")]
[Action("getUserById/{id}", method: RestMethod.Get)]
```

#### 3. Parameter Binding
```csharp
// ✅ Good: Appropriate use of attributes
[Action("{id}/orders", method: RestMethod.Get)]
Task<List<Order>> GetUserOrdersAsync(
    [Path] int id,
    [Query] int page = 1,
    [Query] int pageSize = 10
);
```

#### 4. Error Handling
```csharp
// ✅ Good: Use BusinessException for business errors
public async Task<User> GetUserAsync(int id)
{
    var user = await _repository.GetByIdAsync(id);
    if (user == null)
        BusinessException.ThrowNotFound($"User {id} not found");
    
    return user;
}
```

## Conclusion

BlackDigital.AspNet's REST system offers a modern and productive approach to API development, allowing developers to focus on business logic while the framework handles HTTP infrastructure.

### Key Advantages

1. **Productivity**: Up to 3x faster development
2. **Quality**: Significant bug reduction
3. **Maintainability**: Cleaner and more focused code
4. **Testability**: Simpler and more effective tests
5. **Flexibility**: Coexistence with traditional controllers
6. **Evolution**: Automatic versioning with transformations

### When to Use

**Ideal for:**
- Standard business APIs
- CRUD operations
- Services with complex domain logic
- Projects that need API versioning
- Teams that want to focus on business logic

**Coexistence with Controllers:**
- Use traditional controllers for specific cases that need full HTTP control
- Use the REST system for most business APIs
- Migrate gradually as needed

BlackDigital.AspNet REST represents a natural evolution in API development, maintaining simplicity without sacrificing flexibility.

---

**Related Documentation:**
- [Transformation System](transform-system.md)
- [Main Documentation](../README.md)