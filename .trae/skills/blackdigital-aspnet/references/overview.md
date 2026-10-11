# BlackDigital.AspNet Overview

## Purpose

BlackDigital.AspNet is an ASP.NET Core framework that simplifies REST API development by replacing traditional controllers with attribute-driven service interfaces. It provides a REST middleware, an API transformation/versioning engine, OpenAPI + Scalar documentation generation, JWT and Argon2id security services, and SMTP/HTML-template email infrastructure.

It depends on the shared `BlackDigital` library (for `Id`, `BusinessException`, `JsonCast`, REST attributes, etc.).

## Main source areas

- `src/Rest/`
  - `RestMiddleware` — intercepts HTTP requests and routes them to `[Service]`/`[Action]`-decorated interface methods
  - `RestRouteRegistry` — pre-built route table (singleton) mapping HTTP methods to service methods
  - `RestServiceBuilder` / `RestMiddlewareExtensions` — DI registration (`AddRestServices`, `UseRestMiddleware`)
  - `RestHelper` — MVC configuration (`AddRestMvcOptions`, `AddDefaultOptions`)
  - `RestError` — internal 500-error payload shape
- `src/Rest/Transforms/`
  - `TransformManager`, `TransformBuilder`, `TransformKey`, `TransformDirection`, `TransformRule`, `ITransformRule`, `TransformExtensions`
  - request/response body transformation and date-based API versioning
- `src/OpenApi/`
  - `BdOpenApiDocumentBuilder` — builds the OpenAPI document from `[Service]` interfaces
  - `BdOpenApiSchemaGenerator` — reflection-based schema generator (pure `Microsoft.OpenApi`)
  - `BdOpenApiOptions` — configuration (title, version, security schemes, assembly filters, etc.)
  - `OpenApiDocumentProvider` / `IOpenApiDocumentProvider` — lazily-built, cached document
  - `OpenApiEndpointRouteBuilderExtensions` — `AddOpenApi` / `MapOpenApi` (Scalar UI)
  - `ServiceAttributeHelper` / `ActionAttributeHelper` — reflection helpers for route/method/authorize metadata
- `src/Security/`
  - `JwtService` / `ITokenService` — JWT generation, validation, refresh tokens, token id extraction
  - `Argon2idPasswordHashService` / `IPasswordHashService` — Argon2id password hashing and verification
- `src/Infrastructures/`
  - `IEmailService`, `Email`, `BaseEmailService`, `SmtpEmailService` — SMTP sending via MailKit with multi-provider fallback
  - `ITemplateEmailService`, `TemplateEmail`, `BaseTemplateEmailService`, `HtmlTemplateEmailService` — HTML template email with `{{ key }}` placeholders
  - `InfrastructureHelper` — `AddSmtpService` / `AddEmailService<T>`
- `src/Binder/` — `IdBinder`, `IdModelBinderProvider` (model binding for `BlackDigital.Model.Id`)
- `src/Constraint/` — `IdConstraint` (route constraint for `id` values)
- `src/Filters/` — `ModelStateValidationFilter` (returns `ErrorApiResponse` on invalid model state)
- `src/Services/`
  - `ServiceHelper` — DI helpers for email template, JWT, Argon2id, and combined `ConfigServices`
  - `BdHttpContextHelper` — client IP and JWT `jti` lookup from `IHttpContextAccessor`

## Main docs

- `README.md`
- `README.pt.md`
- `doc/rest-system.md` / `doc/rest-system.pt.md`
- `doc/transform-system.md` / `doc/transform-system.pt.md`
- `doc/openapi.md` / `doc/openapi.pt.md`
- `doc/security.md` / `doc/security.pt.md`
- `doc/email.md` / `doc/email.pt.md`

## Source-first files to consult

- REST behavior
  - `src/Rest/RestMiddleware.cs`
  - `src/Rest/RestRouteRegistry.cs`
  - `src/Rest/RestMiddlewareExtensions.cs`
  - `src/Rest/RestHelper.cs`
- Transformation behavior
  - `src/Rest/Transforms/TransformManager.cs`
  - `src/Rest/Transforms/TransformBuilder.cs`
  - `src/Rest/Transforms/TransformKey.cs`
  - `src/Rest/Transforms/TransformRule.cs`
- OpenAPI behavior
  - `src/OpenApi/BdOpenApiDocumentBuilder.cs`
  - `src/OpenApi/BdOpenApiSchemaGenerator.cs`
  - `src/OpenApi/BdOpenApiOptions.cs`
  - `src/OpenApi/OpenApiEndpointRouteBuilderExtensions.cs`
- Security behavior
  - `src/Security/JwtService.cs`
  - `src/Security/Argon2idPasswordHashService.cs`
- Email behavior
  - `src/Infrastructures/SmtpEmailService.cs`
  - `src/Infrastructures/HtmlTemplateEmailService.cs`
  - `src/Infrastructures/Email.cs`
  - `src/Infrastructures/TemplateEmail.cs`

## Common LLM mistakes to avoid

- Generating controller-based examples instead of `[Service]`/`[Action]` interface-based services
- Forgetting that `UseRestMiddleware()` must come **before** `UseAuthorization()`
- Assuming `[Query]` only binds simple scalars — it also expands complex objects property-by-property
- Treating `BusinessException` like a generic exception (it maps to its own HTTP code)
- Hardcoding the Bearer security scheme or the OpenAPI title instead of using `BdOpenApiOptions`
- Using short JWT secrets (the service requires at least 32 bytes)
- Generating transformation rules without the `TransformRule<TIn, TOut>` base class
- Forgetting that the transformation key is `"{METHOD}:{BaseRoute}/{ActionRoute}"`
- Placing email templates outside `{contentRoot}/templates/email/`
