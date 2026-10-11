# Example Project Roadmap

The runnable example lives in `example/` and demonstrates how consumers use `BlackDigital.AspNet`. Currently it covers REST services, transformations, OpenAPI, and Razor Pages, but it leaves many framework features undemonstrated.

This is the **priority list of features to add to the example project** when working on related areas.

## Current state

The example has:
- One service interface: `IUser` (`api/user`) with `GetUserAsync` (GET) and `SaveUserAsync` (POST)
- Transformation rules: `SaveUserTransformRule` (input/output), `OldSaveUsertoSaveUserTransformRule`, `SaveUserIdTransformRule`
- OpenAPI configured with title, version, security schemes, Scalar
- Razor Pages (Index, Privacy, Error)
- No auth, no email, no security, no complex binding demos

## High-priority additions

### 1. Authentication and JWT

**Why:** The framework ships `JwtService` and `[Action(authorize: true)]`, but the example only uses `authorize: false`. Users never see how auth flows end-to-end.

**What to add:**
- Register `JwtService` via `AddJwtService(builder.Configuration)`
- Add a `Jwt` section to `appsettings.json`
- Create a `Login` action that validates credentials and returns a JWT
- Add at least one action with `authorize: true` to show protected routes
- Configure `AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(...)` so `UseAuthorization()` works

### 2. Password hashing

**Why:** `Argon2idPasswordHashService` exists but is never shown in context.

**What to add:**
- Register `AddArgon2idPasswordHashService()`
- In the login flow, hash a password on registration and verify on login
- Show the self-describing `$argon2id$...` hash format in a comment or log

### 3. Email services

**Why:** `SmtpEmailService` and `HtmlTemplateEmailService` are core infrastructure but absent from the example.

**What to add:**
- Register `AddSmtpService()` and `AddHtmlTemplateEmailService()`
- Add an `Email` section to `appsettings.json` (with dev SMTP settings or a note)
- Create a "forgot password" or "welcome email" action that sends a templated email
- Add a sample HTML template under `example/templates/email/welcome.html` with `{{ name }}` placeholders

### 4. All parameter binding types

**Why:** The example only uses `[Path]` and `[Body]`. Users don't see `[Query]`, `[Header]`, default (query) binding, or `CancellationToken`.

**What to add:**
- An action with `[Query]` on a complex filter object (demonstrates property-by-property binding)
- An action with `[Header]` (e.g. `[Header] string xTraceId`)
- An action that takes a `CancellationToken` parameter
- An action with a simple unbound query parameter (shows default binding)

### 5. `Id` type in routes

**Why:** The framework has an `Id` model binder and route constraint, but the example uses `string name`.

**What to add:**
- An action with `[Path] Id id` (e.g. `GET api/user/{id}`)
- Show the `id` route constraint working

### 6. `BusinessException` usage

**Why:** `BusinessException` maps to HTTP status codes automatically, but the example has it commented out.

**What to add:**
- Uncomment and demonstrate `BusinessException.ThrowNotFound()` (or a custom code) in `GetUserAsync`
- Show that it returns the correct status code and message

### 7. More HTTP methods and a second service

**Why:** The example only has GET and POST on one service.

**What to add:**
- `PUT`, `DELETE`, `PATCH` actions on `IUser`
- A second service (e.g. `IProduct`) to show multi-service registration and OpenAPI tagging

### 8. ModelState validation

**Why:** `ModelStateValidationFilter` is registered by `AddRestMvcOptions()` but never demonstrated.

**What to add:**
- Add data annotations to `SaveUser` (e.g. `[Required]`, `[EmailAddress]`, `[StringLength]`)
- Show that an invalid body returns `400` with `ErrorApiResponse`

### 9. Dependency injection in service implementations

**Why:** `UserImplemention` has no constructor dependencies.

**What to add:**
- Inject `ILogger<UserImplemention>` or `IEmailService` into `UserImplemention`
- Show that scoped services are resolved per request

### 10. `BdHttpContextHelper` (client IP / token id)

**Why:** The helper extension methods are useful but invisible.

**What to add:**
- Inject `IHttpContextAccessor` into a service
- Use `GetCurrentIpAddress()` and `GetCurrentTokenIdentifier()` in a "whoami" action

## Lower-priority / nice-to-have

### 11. Transformation versioning demonstration

**What:** Add a client-side note or test showing that `?api-version=2020-10-10` returns the old response shape while the latest version returns the new one.

### 12. Multiple email providers

**What:** Show `email.UseProvider("Outlook")` and per-provider SMTP config in `appsettings.json`.

### 13. OpenAPI customization

**What:** Demonstrate overriding `SecuritySchemes`, adding a custom contact/license, and toggling `Enabled` per environment (already partially shown).

### 14. Error response shape

**What:** Show the `RestError` JSON shape for `500` errors in Development (with `stack`) vs Production (without).

## Adding to the example

When adding a feature to the example:

1. Register the service in `Program.cs`
2. Add the interface method in `example/Services/`
3. Implement it in the corresponding `*Implementation.cs`
4. Add any needed models in `example/Models/`
5. Update `appsettings.json` if config is needed
6. Add a brief comment in the code explaining what framework feature it demonstrates
7. Verify it runs: `dotnet run --project example`
