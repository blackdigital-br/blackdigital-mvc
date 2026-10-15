# Transformation System - BlackDigital.AspNet

[🇧🇷 Português](transform-system.pt.md) | 🇺🇸 English

## Introduction: API Evolution with Guaranteed Compatibility

The **Transformation System** of BlackDigital.AspNet was created to solve one of the biggest challenges in API development: **how to evolve an API without breaking existing applications**.

### The API Evolution Problem

When an API evolves, dilemmas arise:
- **Breaking compatibility**: Forces all clients to update code simultaneously
- **Maintaining old versions**: Generates code duplication and maintenance complexity
- **Versioning endpoints**: Multiplies routes and controllers

### The Solution: Automatic Transformation

The transformation system allows your API to **evolve naturally** while **maintaining automatic compatibility** with previous versions:

```csharp
// API v1 (old format) - continues working
POST /api/users
{
  "firstName": "John",
  "lastName": "Doe",
  "email": "john@email.com"
}

// API v2 (new format) - natural evolution
POST /api/users  
{
  "fullName": "John Doe",
  "email": "john@email.com",
  "preferences": { "language": "en-US" }
}

// The system automatically transforms v1 → v2 on input
// And transforms v2 → v1 on output when necessary
```

### Main Benefits

✅ **Doesn't break existing applications** - Old clients continue working  
✅ **Gradual evolution** - New features without impact on legacy code  
✅ **Automatic compatibility** - Transparent transformations between versions  
✅ **Reduced maintenance** - Single implementation for multiple versions  
✅ **Smooth migration** - Clients can migrate at their own pace  

### How It Works

1. **Client makes call** with old format (v1)
2. **System identifies** version based on headers/parameters
3. **Input transformation** converts v1 → v2 automatically
4. **Business logic** processes data in current format (v2)
5. **Output transformation** converts v2 → v1 for the client
6. **Client receives** response in expected format (v1)

### Key Features

- **Intelligent Versioning**: Applies sequential transformations between versions
- **Flexible Direction**: Transformations on input and/or output
- **Automatic Chaining**: Multiple transformations applied sequentially
- **DI Integration**: Full support for dependency injection system
- **Optimized Performance**: Support for synchronous and asynchronous operations

## Practical Example: User API Evolution from v1 to v2

### Real Scenario: User API Evolving from v1 to v2

**Situation**: Your user API needs to evolve, but you have mobile and web applications already in production that cannot stop working.

#### API v1 (Original Format)
```csharp
// Model v1 - separate format
public class UserV1
{
    public int Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public bool IsActive { get; set; }
}

// Endpoint v1
POST /api/users
{
  "firstName": "John",
  "lastName": "Doe", 
  "email": "john@email.com",
  "isActive": true
}
```

#### API v2 (Natural Evolution)
```csharp
// Model v2 - optimized format
public class UserV2
{
    public int Id { get; set; }
    public string FullName { get; set; }  // Combined
    public string Email { get; set; }
    public string Status { get; set; }    // Enum string instead of bool
    public UserPreferences Preferences { get; set; } // New field
}

// Endpoint v2 (same route!)
POST /api/users
{
  "fullName": "John Doe",
  "email": "john@email.com", 
  "status": "Active",
  "preferences": {
    "language": "en-US",
    "theme": "dark"
  }
}
```

### Transformation Implementation

#### 1. Input Transformation (v1 → v2)
```csharp
public class UserV1ToV2InputRule : TransformRule<UserV1, UserV2>
{
    public override UserV2? Transform(UserV1? userV1)
    {
        if (userV1 == null) return null;

        return new UserV2
        {
            Id = userV1.Id,
            FullName = CombineNames(userV1.FirstName, userV1.LastName),
            Email = userV1.Email,
            Status = userV1.IsActive ? "Active" : "Inactive",
            Preferences = new UserPreferences 
            { 
                Language = "en-US", // Default for v1 data
                Theme = "light"     // Default for v1 data
            }
        };
    }

    private string CombineNames(string firstName, string lastName)
    {
        return $"{firstName?.Trim()} {lastName?.Trim()}".Trim();
    }
}
```

#### 2. Output Transformation (v2 → v1)
```csharp
public class UserV2ToV1OutputRule : TransformRule<UserV2, UserV1>
{
    public override UserV1? Transform(UserV2? userV2)
    {
        if (userV2 == null) return null;

        var (firstName, lastName) = SplitFullName(userV2.FullName);

        return new UserV1
        {
            Id = userV2.Id,
            FirstName = firstName,
            LastName = lastName,
            Email = userV2.Email,
            IsActive = userV2.Status == "Active"
        };
    }

    private (string firstName, string lastName) SplitFullName(string fullName)
    {
        if (string.IsNullOrEmpty(fullName)) return ("", "");
        
        var parts = fullName.Split(' ', 2);
        return (parts[0], parts.Length > 1 ? parts[1] : "");
    }
}
```

#### 3. Transformation Registration
```csharp
// Startup.cs or Program.cs
builder.Services.AddTransform(config =>
{
    // Input transformation: v1 → v2
    config.AddInputRule<UserV1ToV2InputRule>("POST:api/users/", "2024-02-01");
    
    // Output transformation: v2 → v1  
    config.AddOutputRule<UserV2ToV1OutputRule>("GET:api/users/", "2024-02-01");
    config.AddOutputRule<UserV2ToV1OutputRule>("POST:api/users/", "2024-02-01");
});
```

For complete documentation with all examples and technical details, please refer to the full content above.

## Configuration and Usage

### Transform Direction

The `TransformDirection` enum (a `[Flags]` enum) controls whether a rule applies to the request body, the response body, or both:

```csharp
[Flags]
public enum TransformDirection
{
    Input  = 1,   // Transform the request body (before the service method runs)
    Output = 2,   // Transform the response body (after the service method runs)
    Both   = Input | Output   // Apply to both input and output
}
```

The `TransformBuilder` exposes dedicated helpers for each direction:

```csharp
builder.Services.AddTransform(config =>
{
    // Input only — request body
    config.AddInputRule<MyInputRule>("POST:api/users/", "2024-01-01");

    // Output only — response body
    config.AddOutputRule<MyOutputRule>("GET:api/users/", "2024-01-01");

    // Both input and output
    config.AddInputAndOutputRule<MyRule>("POST:api/users/", "2024-01-01");

    // Generic overload with explicit direction
    config.AddRule<MyRule>("POST:api/users/", "2024-01-01", TransformDirection.Input);
});
```

### Transform Key

Every rule is registered under a `TransformKey`, a value type composed of three parts:

| Part | Description | Example |
|------|-------------|---------|
| `Key` | The action identifier, built as `Method:BaseRoute/ActionRoute` | `POST:api/users/` |
| `Version` | The version string (typically a date) | `2024-01-01` |
| `Direction` | `Input`, `Output`, or `Both` | `Input` |

```csharp
var key = new TransformKey("POST:api/users/", "2024-01-01", TransformDirection.Input);
```

### How the Version Is Resolved at Request Time

For each request, the REST middleware determines the requested version by checking, in order:

1. **Query string**: `?api-version=2024-01-01`
2. **Header**: `x-api-version: 2024-01-01`
3. **Fallback default**: `1970-01-01` (when neither is present)

### Rule Chaining

`TransformManager.GetRequiredRules(key)` returns **all rules whose version is greater than the requested version**, sorted by version:

- **Input direction**: ascending order (oldest → newest), so the request body evolves step-by-step toward the current model.
- **Output direction**: descending order (newest → oldest), so the response body de-evolves from the current model back to the requested version.

A rule registered with `TransformDirection.Both` matches both `Input` and `Output` queries.

### Declarative Rules with `[Transform]` Attribute

In addition to registering rules imperatively in `Program.cs` via `AddTransform`, you can declare a transform directly on the action method using the `[Transform]` attribute. The rule is registered automatically when `AddRestServices` scans the service interfaces:

```csharp
[Service("api/users")]
public interface IUserService
{
    [Action("", method: RestMethod.Post)]
    [Transform(typeof(UserV1ToV2Rule), "2024-01-01", TransformDirection.Input)]
    Task<User> CreateAsync([Body] User user);

    [Action("", method: RestMethod.Get)]
    [Transform(typeof(UserV2ToV1Rule), "2024-01-01", TransformDirection.Output)]
    Task<List<User>> GetAllAsync();
}
```

The action key is derived automatically from the service base route and action route (matching the key used at runtime by the REST middleware), so you do not need to specify it manually.

`[Transform]` and `AddTransform` share the same `TransformBuilder` registry, so you can mix declarative and programmatic rules freely.

### No Duplicate Versions

For the same action key and direction, **two rules may not share the same version**. Because the execution order within a single version would be undefined, the framework throws `InvalidOperationException` when you try to register a duplicate `(key, version, direction)` tuple. Use distinct versions to chain transformations:

```csharp
// OK — different versions chain in order
config.AddInputRule<Step1Rule>("POST:api/users/", "2024-01-01");
config.AddInputRule<Step2Rule>("POST:api/users/", "2024-06-01");

// Throws InvalidOperationException — same key, version and direction
config.AddInputRule<Step1Rule>("POST:api/users/", "2024-01-01");
config.AddInputRule<Step2Rule>("POST:api/users/", "2024-01-01");
```

### Why Wildcard Keys Are Not Supported

The transform system is intentionally **scoped to individual actions** and does not support wildcard keys (e.g. `POST:api/*`). Transformations change the input and output models of a specific action when its contract evolves between versions. Applying a rule across many actions by wildcard is dangerous because it can silently alter the models of unrelated endpoints, producing inconsistent or broken contracts. If you need the same transformation on several actions, register it explicitly for each action key.

### Rule Interface Hierarchy

Rules implement one of the generic interfaces (or extend the matching abstract base class):

```
ITransformRule                  (non-generic: object? Transform(object?))
├── ITransformRule<T>           (same type in/out)
└── ITransformRule<TIn, TOut>   (different types)
```

The abstract base classes provide the boilerplate so you only override the typed `Transform` method:

```csharp
// Same type (e.g. normalise a value)
public class NormaliseUserRule : TransformRule<User>
{
    public override User? Transform(User? value)
    {
        if (value != null) value.Email = value.Email.ToLowerInvariant();
        return value;
    }
}

// Different types (e.g. v1 → v2)
public class UserV1ToV2Rule : TransformRule<UserV1, UserV2>
{
    public override UserV2? Transform(UserV1? value) => /* ... */;
}
```

Rules are instantiated through the DI container when registered by type (`AddRule<TRule>(...)`), so they can accept constructor dependencies.

---

**Related Documentation:**
- [REST System](rest-system.md)
- [Main Documentation](../README.md)