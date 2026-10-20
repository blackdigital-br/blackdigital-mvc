# Testing Reference

## Test stack

- **xUnit** (`xunit` 2.9.3)
- **Moq** (`Moq` 4.21.0)
- **coverlet.collector** (10.1.0) for coverage
- `Microsoft.NET.Test.Sdk` 18.10.1
- Target framework: `net10.0`

Test project: `test/BlackDigital.AspNet.Test.csproj`. It references `..\src\BlackDigital.AspNet.csproj`.

## Folder structure

Tests mirror `src/`:

```
test/
├── Binder/
│   ├── IdBinderTest.cs
│   └── IdModelBinderProviderTest.cs
├── Constraint/
│   └── IdConstraintTest.cs
├── Rest/
│   ├── RestHelperTest.cs
│   ├── RestMiddlewareExtensionsTest.cs
│   ├── RestMiddlewareTest.cs
│   ├── RestServiceBuilderTest.cs
│   └── Transforms/
│       ├── TransformBuilderTest.cs
│       ├── TransformDirectionHelperTest.cs
│       ├── TransformDirectionTest.cs
│       ├── TransformExtensionsTest.cs
│       ├── TransformKeyTest.cs
│       ├── TransformManagerTest.cs
│       └── TransformRuleTest.cs
```

## Naming conventions

- Class: `{SourceClassName}Test` (e.g. `RestMiddlewareTest`)
- Method: `{MethodUnderTest}_{Condition}_{ExpectedResult}`
  - `Constructor_WhenNextIsNull_ShouldThrowArgumentNullException`
  - `GetRequiredRules_WithOutputDirection_ShouldReturnDescendingOrder`
  - `InvokeAsync_WhenNoRouteMatches_ShouldCallNextMiddleware`

## Arrange / Act / Assert

Use the AAA pattern with comments:

```csharp
[Fact]
public void Constructor_WhenSecretKeyTooShort_ShouldThrowArgumentException()
{
    // Arrange
    var shortKey = "short";

    // Act & Assert
    Assert.Throws<ArgumentException>(() =>
        new JwtTokenService(shortKey, "issuer", "audience"));
}
```

## Mocking patterns

Mock `ILogger<T>` by verifying `Log` calls:

```csharp
_mockLogger.Verify(
    x => x.Log(
        LogLevel.Information,
        It.IsAny<EventId>(),
        It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("expected message")),
        It.IsAny<Exception>(),
        It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
    Times.Once);
```

Mock `HttpContext` by composing `HttpRequest`, `HttpResponse`, `IServiceProvider`, and `ClaimsPrincipal`:

```csharp
var mockHttpContext = new Mock<HttpContext>();
var mockRequest = new Mock<HttpRequest>();
var mockResponse = new Mock<HttpResponse>();
mockHttpContext.Setup(x => x.Request).Returns(mockRequest.Object);
mockHttpContext.Setup(x => x.Response).Returns(mockResponse.Object);
mockHttpContext.Setup(x => x.RequestServices).Returns(mockServiceProvider.Object);
```

## Testing internal members

Use reflection for non-public methods (as in `RestMiddlewareTest`):

```csharp
var method = typeof(RestMiddleware)
    .GetMethod("ConvertValue", BindingFlags.NonPublic | BindingFlags.Instance);
var result = method?.Invoke(middleware, new object[] { "123", typeof(int) });
```

## Coverage gaps — tests that do NOT exist yet

These modules have **zero or incomplete** test coverage. Adding tests here is high-value work:

### OpenApi (no tests at all)
- `BdOpenApiOptions` — defaults, `Clone()`, collection initialization
- `BdOpenApiDocumentBuilder` — service/action discovery, route building, parameter mapping, response mapping, security requirements
- `BdOpenApiSchemaGenerator` — primitive, enum, collection, dictionary, nullable, cycle handling
- `OpenApiDocumentProvider` — caching, per-scheme+host document generation
- `OpenApiEndpointRouteBuilderExtensions` — `AddOpenApi`, `MapOpenApi`, `Enabled` gating
- `ServiceAttributeHelper` / `ActionAttributeHelper` — route/method resolution
- `AuthorizeAttributeHelper` — implementation resolution and `[Authorize]` requirement discovery

### Security (no tests at all)
- `JwtTokenService` (from `BlackDigital.Domain`) — secret key length validation, token generation, validation, refresh token randomness, `jti` extraction, expiry checks
- `Argon2idPasswordHashService` — parameter validation, hash format, round-trip verification, wrong-password rejection, malformed hash handling

### Infrastructures (partial)
- `Email` / `TemplateEmail` (from `BlackDigital.Domain`) — fluent builder API
- `HtmlTemplateEmailService` (from `BlackDigital.Domain`) — template loading, placeholder replacement, path traversal rejection
- `InfrastructureHelper` — DI registration

### Services (no tests)
- `ServiceHelper` — all `Add*` extension methods register correct types and lifetimes
- `BdHttpContextHelper` — IP resolution, `jti` extraction

### Filters (no tests)
- `ModelStateValidationFilter` — short-circuits on invalid model state, builds `ErrorApiResponse`

## Running tests

```powershell
dotnet test
```

With coverage:

```powershell
dotnet test --collect:"XPlat Code Coverage"
```
