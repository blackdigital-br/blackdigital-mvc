# Infrastructure Reference

## Email services

The email infrastructure (`IEmailService`, `Email`, `SmtpEmailService`, `ITemplateEmailService`, `TemplateEmail`, `HtmlTemplateEmailService`) now lives in the **`BlackDigital.Domain`** package (namespace `BlackDigital.Domain.Infrastructures`). This library provides only the DI registration helpers below.

### Registration

```csharp
// SMTP sender (MailKit)
builder.Services.AddSmtpService();

// HTML template email service (depends on IEmailService)
builder.Services.AddHtmlTemplateEmailService();

// Or with a custom implementation
builder.Services.AddEmailService<MyCustomEmailService>();
builder.Services.AddEmailTemplate<MyTemplateEmailService>();
```

`AddSmtpService()` registers `SmtpEmailService` as `IEmailService` (scoped).
`AddHtmlTemplateEmailService()` registers `HtmlTemplateEmailService` as `ITemplateEmailService` (scoped).

### `Email` fluent API

```csharp
var email = Email.Create()
    .To("user@example.com", "User Name")
    .To(new[] { "a@x.com", "b@x.com" })
    .WithSubject("Welcome")
    .WithBody("<h1>Hi</h1>", isHtml: true)
    .WithProvider("Outlook");

await emailService.SendEmailAsync(email);

// Or with a builder lambda
await emailService.SendEmailAsync(e => e
    .To("user@example.com")
    .WithSubject("Hi")
    .WithBody("Hello"));
```

`Email` properties: `Provider`, `Tos` (dictionary of email → display name), `Subject`, `Body`, `IsHtml` (default `true`).

### `TemplateEmail`

`TemplateEmail` extends `Email` with:
- `TemplateName` — name of the template file (without path; `.html` appended if missing)
- `Parameters` — dictionary of placeholder values

```csharp
var email = new TemplateEmail()
    .UseTemplate("welcome")
    .AddParameter("name", "John")
    .AddParameters(new Dictionary<string, string> { { "code", "123" } })
    .To("user@example.com")
    .WithSubject("Welcome");

await templateEmailService.SendTemplateEmailAsync(email);
```

### SmtpEmailService

Reads SMTP settings from `IConfiguration`. For a given provider (from `email.Provider` or `SetProvider`), it reads:

- `Email:{Provider}:Host`, `Email:{Provider}:Port`, `Email:{Provider}:User`, `Email:{Provider}:Password`, `Email:{Provider}:From`, `Email:{Provider}:Name`, `Email:{Provider}:UseSsl`, `Email:{Provider}:UseStartTls`

Any missing per-provider key falls back to the default `Email:{Key}` section.

Default port is `587` when not configured.

**Secure socket resolution** (per-provider flags take precedence):
- `UseSsl=true` → `SslOnConnect`
- `UseStartTls=true` → `StartTls`
- port `465` → `SslOnConnect`
- port `587` → `StartTls`
- otherwise → `Auto`

If `Host`, `User`, `Password`, or `From` are missing, the email is **not sent** (a warning is logged).

### HtmlTemplateEmailService

- Loads templates from `{contentRoot}/templates/email/{TemplateName}.html`.
- Rejects template names containing `..`, `/`, or `\` (path traversal protection).
- Replaces `{{ key }}` placeholders with values from `Parameters`; unmatched placeholders are removed.
- Delegates sending to the injected `IEmailService`.

## Model binding for `Id`

`AddRestMvcOptions()` (in `RestHelper`) registers:

- `IdModelBinderProvider` at index 0 of `MvcOptions.ModelBinderProviders` — `IdBinder` binds `BlackDigital.Model.Id` from the value provider (route, query, form). Empty/whitespace values fail silently.
- the `id` route constraint (`IdConstraint`) via `RouteOptions.ConstraintMap["id"]` — validates that the route value is a string.

## JSON converters

`AddRestMvcOptions()` configures `JsonSerializerOptions`:
- `DefaultIgnoreCondition = WhenWritingNull`
- `IdConverter` (from `BlackDigital.Converters`)
- `TimeSpanConverter` (from `BlackDigital.Converters`)
- `JsonStringEnumConverter`

## ModelState validation filter

`ModelStateValidationFilter` (registered by `AddRestMvcOptions`) runs on every action. When `ModelState` is invalid, it short-circuits with `400 Bad Request` and an `ErrorApiResponse` body built from each model error (`"{key}: {message}"`).

## DI helpers (ServiceHelper)

| Method | Registers |
|--------|-----------|
| `AddEmailTemplate<T>()` | `ITemplateEmailService` → `T` (scoped) |
| `AddTokenService<T>()` | `ITokenService` → `T` (scoped) |
| `AddPasswordService<T>()` | `IPasswordHashService` → `T` (scoped) |
| `AddHtmlTemplateEmailService()` | `ITemplateEmailService` → `HtmlTemplateEmailService` |
| `AddArgon2idPasswordHashService()` | `IPasswordHashService` → `Argon2idPasswordHashService` (default params) |
| `AddArgon2idPasswordHashService(memorySize, iterations, parallelism, saltLength, hashLength)` | `IPasswordHashService` → `Argon2idPasswordHashService` (custom params, singleton) |
| `AddJwtTokenService(IConfiguration)` | `ITokenService` → `JwtTokenService` (from `Jwt` config section) |
| `AddJwtTokenService(secretKey, issuer, audience, expirationMinutes)` | `ITokenService` → `JwtTokenService` (explicit) |
| `ConfigServices<TTemplate, TToken, TPassword>()` | all three generic registrations at once |

## Source files

- `src/Infrastructures/InfrastructureHelper.cs`
- `src/Binder/IdBinder.cs`
- `src/Binder/IdModelBinderProvider.cs`
- `src/Constraint/IdConstraint.cs`
- `src/Filters/ModelStateValidationFilter.cs`
- `src/Services/ServiceHelper.cs`
- `src/Services/BdHttpContextHelper.cs`

> `Email`, `TemplateEmail`, `IEmailService`, `ITemplateEmailService`, `SmtpEmailService`, and `HtmlTemplateEmailService` live in the `BlackDigital.Domain` package (namespace `BlackDigital.Domain.Infrastructures`), not in this repository.
