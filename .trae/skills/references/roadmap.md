# Framework Roadmap

Ideas for improving `BlackDigital.AspNet` itself. These are not user-facing docs; they are candidate features and refactors for framework developers.

## OpenAPI improvements

- Support `[ProducesResponseType]`-style attributes for explicit response codes and schemas
- Support `[FromForm]` / `IFormFile` parameter binding and OpenAPI multipart schema
- Allow custom schema generation callbacks via `BdOpenApiOptions`

## REST improvements

- Add `PATCH` support with JSON Patch (`Microsoft.AspNetCore.JsonPatch`)
- Support `[FromForm]` for multipart form data
- Support `ProblemDetails` for error responses (currently returns `RestError` or plain text)

## Transformation improvements

- Add a fluent builder for chaining input/output rules more readably
- Support conditional rules (e.g. only transform if a header is present)

## Security improvements

- Add refresh-token rotation support in `JwtTokenService` (in `BlackDigital.Domain`)
- Add token blacklist / revocation support
- Support asymmetric signing (RSA/ECDSA) in addition to HMAC
- Add password strength validation helper

## Infrastructure improvements

- Add a Razor/liquid template email service (beyond simple `{{ key }}` replacement)
- Add email attachment support to `Email` / `TemplateEmail`
- Add retry/circuit-breaker for SMTP sending
- Add template caching for `HtmlTemplateEmailService`

## Performance

- Cache `BdOpenApiSchemaGenerator` schemas across documents

## Developer experience

- Add a `dotnet new` template for BlackDigital.AspNet projects
- Add Roslyn analyzers for common mistakes (e.g. `[Service]` on a class instead of interface, missing `[Action]`)
- Add a source generator to register services automatically
- Add `[ApiExplorerSettings]`-style ignore attribute for OpenAPI
