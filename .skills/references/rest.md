# REST System Reference

## Core behavior

- REST services are **interfaces** decorated with `[Service]`; methods are decorated with `[Action]`.
- `RestMiddleware` intercepts requests and matches them against the pre-built route table in `RestRouteRegistry`.
- `RestRouteRegistry` is built once during `AddRestServices(...)` from the registered `IServiceCollection` and registered as a singleton.
- Routes are matched by HTTP method + URL pattern; `{param}` segments are captured as route values.

## Registration

```csharp
builder.Services.AddRestServices(rest =>
{
    rest.AddService<IUserService, UserService>();                 // Scoped (default)
    rest.AddSingletonService<ICacheService, CacheService>();      // Singleton
    rest.AddTransientService<IHelperService, HelperService>();    // Transient
    rest.AddService<ICustomService>(sp => new CustomService(sp)); // Factory (Scoped)
});

builder.Services.AddRestMvcOptions(); // Id binder, id constraint, JSON converters, validation filter

var app = builder.Build();
app.UseRouting();
app.UseRestMiddleware();   // MUST come before UseAuthorization
app.UseAuthorization();
app.MapControllers();      // traditional controllers still work
app.Run();
```

`AddRestMvcOptions()` calls `AddControllers(...)` internally, so do not call `AddControllers()` separately unless you intend to merge options.

## Service and action attributes

### `[Service]`

Defines the base route for all actions in the interface.

```csharp
[Service("api/users")]
public interface IUserService { }
```

`ServiceAttributeHelper.GetRoute(...)` resolves the route from `BaseRoute`, `Route`, `Path`, or `Value` (in that order), falling back to fields and finally `ToString()`.

### `[Action]`

Configures the HTTP method and sub-route.

```csharp
[Action("{id}", method: RestMethod.Get)]
Task<User> GetUserAsync([Path] int id);
```

`ActionAttributeHelper` resolves:
- route from `Path`, `Route`, `Template`, or `Value`
- HTTP method from `Method` or `RestMethod` (defaults to GET)

## Parameter binding

`RestMiddleware.PrepareMethodParameters(...)` binds each parameter by its attribute:

| Attribute | Source | Notes |
|-----------|--------|-------|
| `[Path]` | route segment | `Name` overrides the parameter name |
| `[Body]` | request body | JSON-deserialized; may be transformed |
| `[Header]` | request header | `Name` overrides the header key |
| `[Query]` | query string | simple types bind by name; complex types bind property-by-property |
| *(none)* | query string | same as `[Query]` for simple and complex types |

`CancellationToken` is always bound to `context.RequestAborted`.

Parameters with default values fall back to the declared default when no binding source produces a value.

### Complex query binding

For complex `[Query]` types, each public property is bound from the query string. Supported key styles:
- `prefix.PropertyName` (e.g. `filter.Name=John`)
- `prefix[PropertyName]`
- unprefixed `PropertyName` when no prefix matches

Collections support `key=v1&key=v2`, `key[]=v1&key[]=v2`, and indexed `key[0]=v1&key[1]=v2`.

### Type conversion

`ConvertValue` handles enums, `string`, `int`, `long`, `bool`, `DateTime`, `DateTimeOffset`, `Guid`, `Id`, and falls back to `Convert.ChangeType`.

## Authorization

Access requirements are declared with `[Authorize]` (`BlackDigital.Domain.Security`)
on the service **implementation** — never on the interface, which may be shared with
the front end. The attribute may be placed on the class (applies to every action) or
on a method (applies to that action only); the middleware combines both.

```csharp
public interface IAdminService
{
    Task<HealthStatus> GetHealthAsync();
    Task<string> GetDataAsync();
}

[Authorize]                                             // every action requires an authenticated user
public class AdminService : IAdminService
{
    public Task<HealthStatus> GetHealthAsync() => ...;   // authenticated user

    [Authorize(Policy = "Category.Write")]               // narrower requirement
    public Task<string> GetDataAsync() => ...;
}
```

A requirement is satisfied by:
- no `Roles` and no `Policy` → any authenticated user;
- `Roles` → membership in **at least one** listed role;
- `Policy` → the named ASP.NET Core policy (evaluated through `IAuthorizationService`);
- both → the roles **and** the policy must be satisfied.

Requirements from the class and the method are combined with **OR** semantics: access
is granted when any single requirement is satisfied, so a class-level `[Authorize]`
plus a method-level `[Authorize(Roles = ...)]` lets a caller in with either.

When no requirement is satisfied the middleware writes a `ProblemDetails`
(`application/problem+json`) response: `401 Unauthorized` when the caller is not
authenticated, `403 Forbidden` when authenticated but not allowed. If a `Policy` is
declared but no `IAuthorizationService` is registered, the requirement is not met and
a warning is logged.

`AuthorizeAttributeHelper.GetRequirements(implementationType, interfaceMethod)`
resolves the requirements for an interface method by locating the matching
implementation method (via the interface map).

## Return value handling

`ProcessResult` handles:
- `void` / non-generic `Task` / non-generic `ValueTask` → `204 No Content`
- `Task<T>` / `ValueTask<T>` → awaited, `T` serialized as JSON
- any other value → serialized as JSON
- `null` result → `204 No Content`

Response `Content-Type` is `application/json`. The JSON options come from `IOptions<MvcJsonOptions>` (registered by `AddRestMvcOptions`), defaulting to `JsonSerializerDefaults.Web`.

## Error handling

| Exception | Status | Body |
|-----------|--------|------|
| `BusinessException` | `exception.Code` | `exception.Message` (plain text) |
| `BadHttpRequestException` | `exception.StatusCode` | `exception.Message` (plain text) |
| `TargetInvocationException` wrapping `BusinessException` | inner `Code` | inner `Message` |
| anything else | `500` | `RestError` JSON: `{ "message", "stack" }` |

The `stack` field is only populated when `IWebHostEnvironment.IsDevelopment()` is true.

## Action key (for transformations)

The action key used by the transformation system is:

```
{HTTP_METHOD}:{BaseRoute}/{ActionRoute}
```

Example: for `[Service("api/users")]` + `[Action(method: RestMethod.Post)]`, the key is `POST:api/users/`.

This exact string is what you pass to `AddInputRule` / `AddOutputRule`.

## `Id` type support

`AddRestMvcOptions()` registers:
- `IdModelBinderProvider` at index 0 of MVC model binder providers — binds `BlackDigital.Model.Id` from route/query values
- the `id` route constraint via `RouteOptions.ConstraintMap`
- `IdConverter`, `TimeSpanConverter`, and `JsonStringEnumConverter` in the JSON serializer options
- `ModelStateValidationFilter` — returns `400 Bad Request` with `ErrorApiResponse` when `ModelState` is invalid

## Source files

- `src/Rest/RestMiddleware.cs`
- `src/Rest/RestRouteRegistry.cs`
- `src/Rest/RestMiddlewareExtensions.cs`
- `src/Rest/RestServiceBuilder.cs`
- `src/Rest/RestHelper.cs`
- `src/Rest/RestError.cs`
