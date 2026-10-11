---
name: blackdigital-aspnet
description: Understand and work with the BlackDigital.AspNet framework in this repository. Use when the user asks about BlackDigital.AspNet APIs, REST middleware, OpenAPI/Scalar, transforms, security (JWT/Argon2id), email infrastructure, or code changes in this library. Do not use for unrelated projects or for the core BlackDigital library (use the blackdigital-library skill for that).
---

# BlackDigital.AspNet

Work from the source code first and use the documentation as supporting context.

## Use this skill

Apply this skill when the task is about the `BlackDigital.AspNet` framework in this repository, including:
- explaining REST middleware behavior, routing, parameter binding, or authorization
- writing examples or documentation for REST services, transformations, OpenAPI, security, or email
- implementing or updating features in the framework
- reviewing consistency between `src/`, `README*`, and `doc/`

Do not use this skill for the core `BlackDigital` library (REST client generation, model, data annotations, helpers) — that lives in a separate repository and has its own `blackdigital-library` skill.

## Core rules

1. Treat `src/` as the source of truth when docs and code disagree.
2. Prefer existing framework conventions over generic ASP.NET Core habits.
3. When changing docs or examples, keep English and Portuguese files aligned unless the task explicitly scopes only one language.
4. When you mention framework behavior, cite the real implementation files whenever possible.

## High-signal conventions

### REST system

- REST services are **interfaces** decorated with `[Service]`; methods are decorated with `[Action]`. There are no controllers.
- The route key used by the transformation system is built as `"{METHOD}:{BaseRoute}/{ActionRoute}"` (e.g. `"POST:api/users/"`).
- `[Query]` on a complex type binds property-by-property from the query string (both `prefix.Property` and `prefix[Property]` styles are supported).
- `CancellationToken` parameters are automatically bound to `context.RequestAborted`.
- `BusinessException` (from `BlackDigital`) maps directly to its HTTP status code; unhandled exceptions return `500` with a `RestError` payload whose `stack` is only populated in Development.
- `UseRestMiddleware()` must be placed **before** `UseAuthorization()` in the pipeline.

Read [references/rest.md](references/rest.md) for details before editing REST code or examples.

### Transformation system

- Transformations are keyed by `(actionKey, version, direction)` where `direction` is `Input`, `Output`, or `Both`.
- Version is resolved per request from `?api-version=` query string, then `x-api-version` header, then fallback `1970-01-01`.
- Rules with versions **newer than** the requested version are applied in sequence; input rules are ordered ascending, output rules descending.
- Rules implement `ITransformRule`; the recommended base classes are `TransformRule<T>` and `TransformRule<TIn, TOut>`.

Read [references/transform.md](references/transform.md) before editing transformation code or examples.

### OpenAPI + Scalar

- The document is built with **pure `Microsoft.OpenApi` 3.x** — no `Microsoft.AspNetCore.OpenApi`, no Swashbuckle.
- `BdOpenApiOptions` drives everything: title, version, server URL, assembly include/exclude prefixes, security schemes, Scalar title, and route pattern.
- A default **Bearer JWT** security scheme is registered and applied globally; clear or overwrite `SecuritySchemes` to opt out.
- `Enabled` defaults to `true` — callers must gate it per environment (e.g. `options.Enabled = env.IsDevelopment()`).
- The OpenAPI JSON route defaults to `/openapi/{documentName}.json`; Scalar is mapped via `MapScalarApiReference`.

Read [references/openapi.md](references/openapi.md) before editing OpenAPI code or examples.

### Security

- `JwtService` enforces a **32-byte (256-bit)** minimum secret key for HMAC-SHA256 and uses `ClockSkew = TimeSpan.Zero`.
- `Argon2idPasswordHashService` produces self-describing `$argon2id$v=19$m=...,t=...,p=...$salt$hash` strings and uses fixed-time comparison.
- `AddJwtService(IConfiguration)` reads the `Jwt` section and **fails fast** at registration if `SecretKey`, `Issuer`, or `Audience` are missing.

Read [references/security.md](references/security.md) before editing security code or examples.

### Email, binding, and MVC helpers

- `Email` and `TemplateEmail` use a **fluent builder** API (`Email.Create().To(...).WithSubject(...).WithBody(...)`).
- `SmtpEmailService` resolves per-provider config with fallback to the default `Email:` section; secure socket defaults are implicit SSL on 465, STARTTLS on 587, auto otherwise.
- `HtmlTemplateEmailService` loads templates from `{contentRoot}/templates/email/` with `{{ key }}` placeholders and rejects path traversal in template names.
- `AddRestMvcOptions()` registers the `Id` model binder, the `id` route constraint, `IdConverter`/`TimeSpanConverter`/`JsonStringEnumConverter`, and `ModelStateValidationFilter`.

Read [references/infrastructure.md](references/infrastructure.md) for email, model binding, filters, and DI helpers.

## Documentation workflow

When the task touches documentation:

1. Read the relevant implementation files in `src/`.
2. Read the matching docs in `README.md`, `README.pt.md`, and `doc/`.
3. Fix mismatches before expanding coverage.
4. Prefer examples that compile against the current API shape.
5. Call out non-obvious conventions explicitly when they are easy for users or LLMs to misunderstand.

Read [references/documentation.md](references/documentation.md) before larger documentation edits.

## Reference routing

- Use [references/overview.md](references/overview.md) for the project map and file locations.
- Use [references/rest.md](references/rest.md) for REST middleware, routing, binding, and error handling.
- Use [references/transform.md](references/transform.md) for the transformation/versioning engine.
- Use [references/openapi.md](references/openapi.md) for OpenAPI document generation and Scalar integration.
- Use [references/security.md](references/security.md) for JWT and Argon2id password hashing.
- Use [references/infrastructure.md](references/infrastructure.md) for email, model binders, route constraints, filters, and DI helpers.
- Use [references/documentation.md](references/documentation.md) for documentation maintenance rules.
