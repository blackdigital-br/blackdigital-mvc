# Framework Roadmap

Ideas for improving `BlackDigital.AspNet` itself. These are not user-facing docs; they are candidate features and refactors for framework developers.

## Testing debt

**Priority: highest**

- Add tests for the entire `OpenApi/` module (currently zero tests)
- Add tests for the entire `Security/` module (currently zero tests)
- Add tests for `Services/ServiceHelper.cs` and `Services/BdHttpContextHelper.cs`
- Add tests for `Filters/ModelStateValidationFilter.cs`
- Add tests for `Infrastructures/InfrastructureHelper.cs`
- Add tests for `OpenApi/ServiceAttributeHelper.cs` and `ActionAttributeHelper.cs`

> `Email`, `TemplateEmail`, `HtmlTemplateEmailService`, `JwtTokenService`, and their tests now live in the `BlackDigital.Domain` repository.

## XML documentation debt

**Priority: high**

- Audit all public members for missing `<summary>`, `<param>`, `<returns>`, `<exception>`
- `RestMiddleware.cs` has only 1 `<summary>` — needs comprehensive docs
- `RestRouteRegistry.cs` — needs method-level docs
- `Security/Argon2idPasswordHashService.cs` — document all public methods and thrown exceptions
- `OpenApi/` files — document options, builder methods, schema generation behavior

> `JwtTokenService`, `ITokenService`, `IPasswordHashService`, and all email types now live in the `BlackDigital.Domain` repository.

## OpenAPI improvements

- Support `[ProducesResponseType]`-style attributes for explicit response codes and schemas
- Support `[FromForm]` / `IFormFile` parameter binding and OpenAPI multipart schema
- Generate `requestBody` for `[Query]` complex objects as a dedicated OpenAPI object with expanded properties (currently handled but may need refinement)
- Support operation tags grouping by service namespace
- Add a `Deprecated` flag on `[Action]` that maps to `operation.deprecated`
- Support `[Obsolete]` on service interfaces → deprecated tag
- Allow custom schema generation callbacks via `BdOpenApiOptions`

## REST improvements

- Add `PATCH` support with JSON Patch (`Microsoft.AspNetCore.JsonPatch`)
- Support `[FromForm]` for multipart form data
- Add a `[Cache]` attribute for response caching
- Add `[RateLimit]` attribute support (or integrate with AspNetCoreRateLimit)
- Support `IAsyncEnumerable<T>` streaming responses
- Add request/response logging middleware option
- Support `ProblemDetails` for error responses (currently returns `RestError` or plain text)
- Add `CancellationToken` automatic binding documentation (already works, needs docs + test)
- Support binding `IFormFile` and `IFormFileCollection`

## Transformation improvements

- Add a fluent builder for chaining input/output rules more readably
- Support wildcard action keys (e.g. `POST:api/*`) for cross-cutting transformations
- Add transformation rule priority within the same version
- Support conditional rules (e.g. only transform if a header is present)
- Add a `[Transform]` attribute on actions to declare rules declaratively instead of in `Program.cs`

## Security improvements

- Add refresh-token rotation support in `JwtTokenService` (in `BlackDigital.Domain`)
- Add token blacklist / revocation support
- Support asymmetric signing (RSA/ECDSA) in addition to HMAC
- Add a `[RequireRole]` / `[RequirePolicy]` attribute on `[Action]`
- Add password strength validation helper
- Add `ITokenService` methods for token introspection

## Infrastructure improvements

- Add a SendGrid / Mailgun / Amazon SES email service implementation
- Add a Razor/liquid template email service (beyond simple `{{ key }}` replacement)
- Add email attachment support to `Email` / `TemplateEmail`
- Add retry/circuit-breaker for SMTP sending
- Add template caching for `HtmlTemplateEmailService`

## Performance

- Cache `RestRouteRegistry` route matching (currently iterates all routes per request)
- Use compiled expression trees for method invocation instead of `MethodInfo.Invoke`
- Cache `BdOpenApiSchemaGenerator` schemas across documents
- Avoid allocations in hot path of `RestMiddleware.PrepareMethodParameters`

## Developer experience

- Add a `dotnet new` template for BlackDigital.AspNet projects
- Add Roslyn analyzers for common mistakes (e.g. `[Service]` on a class instead of interface, missing `[Action]`)
- Add a source generator to register services automatically
- Add `[ApiExplorerSettings]`-style ignore attribute for OpenAPI

## Breaking changes to consider

- Replace `RestError` with `ProblemDetails` (RFC 7807) for consistency with ASP.NET Core
- Change the default transformation version from `1970-01-01` to something more intuitive
- Rename `UserImplemention` typo in the example (should be `UserImplementation`)
