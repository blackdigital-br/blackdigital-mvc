# XML Summaries Reference

## Why summaries matter

- Public APIs without XML comments are invisible to IntelliSense and undocumented in the generated OpenAPI doc.
- The framework is consumed by other developers; every public member must be self-documenting.
- `BdOpenApiOptions.Description` and service/action summaries feed the OpenAPI document.

## Required tags

Every **public** member in `src/` must have:

| Member type | Required tags |
|-------------|---------------|
| Class / interface / enum / struct | `<summary>` |
| Enum value | `<summary>` |
| Method | `<summary>`, `<param>` per parameter, `<returns>` (non-void), `<exception>` for each thrown type |
| Constructor | `<summary>`, `<param>` per parameter, `<exception>` |
| Property | `<summary>` |
| Field | `<summary>` |
| Event | `<summary>`, `<event>` |

### Example

```csharp
/// <summary>
/// Sends a JWT-bearing email to the given recipient.
/// </summary>
/// <param name="recipient">The email address to send to. Must not be null or empty.</param>
/// <param name="cancellationToken">Cancellation token for the async operation.</param>
/// <returns>A task that completes when the email has been sent.</returns>
/// <exception cref="ArgumentNullException">Thrown when <paramref name="recipient"/> is null.</exception>
/// <exception cref="BusinessException">Thrown when the SMTP server rejects the message.</exception>
public Task SendWelcomeEmailAsync(string recipient, CancellationToken cancellationToken = default)
{
    // ...
}
```

## Style rules

- Write summaries in **third-person present** ("Gets", "Sends", "Returns").
- Start with a verb.
- Keep `<summary>` to 1–2 sentences; put detail in `<remarks>` if needed.
- `<param>` descriptions should state the constraint or role, not repeat the name.
- Use `<paramref name="foo"/>` when referring to a parameter inside prose.
- Use `<see cref="OtherType"/>` for cross-references.
- For async methods, document the `Task`/`ValueTask` result in `<returns>`.
- For methods that throw, list **every** exception type with the condition that triggers it.

## Current audit status

The following modules have **incomplete or missing** XML documentation and should be prioritized:

| File | `<summary>` count | Status |
|------|-------------------|--------|
| `OpenApi/BdOpenApiOptions.cs` | 17 | Good — review `<param>` and `<returns>` on methods |
| `OpenApi/BdOpenApiDocumentBuilder.cs` | 5 | Incomplete — most public methods lack docs |
| `OpenApi/BdOpenApiSchemaGenerator.cs` | 3 | Incomplete — `GetSchema` overloads need docs |
| `OpenApi/IOpenApiDocumentProvider.cs` | 4 | Good |
| `OpenApi/OpenApiEndpointRouteBuilderExtensions.cs` | 2 | Incomplete |
| `OpenApi/OpenApiDocumentProvider.cs` | 1 | Incomplete |
| `OpenApi/ServiceAttributeHelper.cs` | 0 | **No docs** |
| `OpenApi/ActionAttributeHelper.cs` | 0 | **No docs** |
| `Rest/RestMiddleware.cs` | 1 | **Severely incomplete** — all public/internal methods need docs |
| `Rest/RestServiceBuilder.cs` | 5 | Partial |
| `Rest/RestRouteRegistry.cs` | 3 | Partial |
| `Rest/RestHelper.cs` | 2 | Partial |
| `Rest/RestMiddlewareExtensions.cs` | 2 | Partial |
| `Rest/RestError.cs` | ? | Needs audit |
| `Rest/Transforms/*` | ? | Needs audit |
| `Security/Argon2idPasswordHashService.cs` | 0 | **No docs** |
| `Services/ServiceHelper.cs` | 5 | Partial |
| `Services/BdHttpContextHelper.cs` | 3 | Partial |
| `Infrastructures/InfrastructureHelper.cs` | ? | Needs audit |
| `Binder/IdBinder.cs` | 2 | Partial |
| `Binder/IdModelBinderProvider.cs` | 2 | Partial |
| `Constraint/IdConstraint.cs` | 2 | Partial |
| `Filters/ModelStateValidationFilter.cs` | 1 | Partial |

> `JwtTokenService`, `ITokenService`, `IPasswordHashService`, `SmtpEmailService`, `HtmlTemplateEmailService`, `Email`, `TemplateEmail`, and the `Base*Service` types now live in the `BlackDigital.Domain` repository.

## How to add summaries efficiently

1. Place the cursor on a public member.
2. Type `///` above it — the IDE auto-generates the skeleton tags.
3. Fill in `<summary>`, `<param>`, `<returns>`, `<exception>`.
4. For methods that throw, check the implementation and `throw` statements.
5. For extension methods, document the `this` parameter like any other parameter.

## Enforcement

There is no automated enforcement yet. As a developer working on this codebase:

- **Never** add a new public member without XML comments.
- When modifying a public member, review and update its existing comments.
- If you touch a file with missing docs, add them for the members you change (and ideally the whole file).
