# OpenAPI + Scalar Reference

## Purpose

The OpenAPI module generates a complete OpenAPI 3.1 document directly from `[Service]`/`[Action]`-decorated interfaces and exposes both the raw JSON document and an interactive **Scalar** UI. It is built with **pure `Microsoft.OpenApi` 3.x** — no `Microsoft.AspNetCore.OpenApi` and no Swashbuckle.

## Registration

```csharp
builder.Services.AddOpenApi(options =>
{
    options.Title = "My API";
    options.Version = "v1";
    options.Description = "REST API built with BlackDigital.AspNet";
    options.Enabled = builder.Environment.IsDevelopment() || builder.Environment.IsStaging();
});

var app = builder.Build();
app.MapOpenApi(); // /openapi/{documentName}.json + Scalar UI
```

`MapOpenApi()` maps:
- the OpenAPI JSON document at `options.OpenApiRoutePattern` (default `"/openapi/{documentName}.json"`)
- the Scalar interactive UI via `MapScalarApiReference`

When `options.Enabled` is `false`, `MapOpenApi()` returns early and registers no routes.

## BdOpenApiOptions

| Property | Default | Description |
|----------|---------|-------------|
| `Title` | `"API"` | `info.title` |
| `Version` | `"v1"` | `info.version` |
| `Description` | `null` | `info.description` |
| `Contact` | `null` | `info.contact` |
| `License` | `null` | `info.license` |
| `ServerUrl` | `null` | when null, derived from the current request (scheme + host + pathBase) |
| `DocumentName` | `"v1"` | used in the JSON route and as the Scalar document id |
| `Enabled` | `true` | gates route registration |
| `IncludedAssemblyPrefixes` | empty | when set, only assemblies whose name starts with one of these are scanned |
| `ExcludedAssemblyPrefixes` | BCL + vendor list | always excluded (System, Microsoft, BlackDigital.AspNet, etc.) |
| `SecuritySchemes` | Bearer JWT | `components.securitySchemes` |
| `ApplyBearerAsGlobalSecurityRequirement` | `true` | adds `security: [{ Bearer: [] }]` at document level |
| `PreferredSecuritySchemes` | `["Bearer"]` | pre-selected in the Scalar auth panel |
| `ScalarTitle` | `Title` | Scalar UI browser-tab title |
| `OpenApiRoutePattern` | `"/openapi/{documentName}.json"` | JSON document route |

`Clone()` creates a shallow copy with new collections so the provider can inject a request-derived `ServerUrl` without mutating the shared singleton options.

## Document discovery

1. `OpenApiDocumentProvider.GetDocument(context)` builds and caches the document per scheme+host.
2. `BdOpenApiDocumentBuilder.Build()` scans `AppDomain.CurrentDomain.GetAssemblies()` using the include/exclude prefix policy.
3. Interfaces with `[ServiceAttribute]` become **tags**; methods with `[ActionAttribute]` become **operations**.

## Tag and operation naming

- **Tag name**: `serviceAttr.GetServiceName()` if set, otherwise the interface name with the leading `I` stripped, `Service`/`AppService` suffix removed, and pluralized (e.g. `IUserService` → `Users`, `ICategoryService` → `Categories`).
- **Operation summary**: method name with `Async` suffix removed and spaces inserted at uppercase/digit boundaries (e.g. `GetUserByIdAsync` → `Get User By Id`).
- **OperationId**: `{DeclaringType.Name}_{MethodName}`.

## Route and parameter mapping

- Full route = `/` + `{baseRoute}/{actionRoute}` (segments joined, trimmed).
- `[Body]` parameter → `requestBody` with `application/json` schema.
- `[Path]` parameter → path parameter (required).
- `[Query]` parameter → for complex types, each public property becomes a query parameter; for simple types it would be a single query parameter (the builder expands properties property-by-property in the current implementation).
- Return type `Task<T>` / `ValueTask<T>` → `200` response with `application/json` schema; `void`/`Task`/`ValueTask` → `204 No Content`.

## Authorization in the document

An operation gets the Bearer security requirement when the service **implementation**
declares at least one `[Authorize]` (`BlackDigital.Domain.Security`) on the class or on
the matching method. Because the attribute lives on the implementation and the builder
only scans interfaces, `AuthorizeAttributeHelper.FindImplementation(...)` resolves the
concrete type among the scanned candidates and `HasRequirements(...)` checks its
requirements. This matches the runtime check in `RestMiddleware`. The global Bearer
requirement (when `ApplyBearerAsGlobalSecurityRequirement` is true) applies to every operation.

## Schema generation

`BdOpenApiSchemaGenerator` converts .NET types to `IOpenApiSchema`:

- primitives → `integer/int32`, `integer/int64`, `number/float`, `number/double`, `boolean`, `string`
- `DateTime`/`DateTimeOffset` → `string/date-time`
- `Guid` → `string/uuid`, `Uri` → `string/uri`, `DateOnly` → `string/date`, `TimeOnly` → `string/time`, `byte[]` → `string/byte`
- enums → `string`
- `IDictionary<string, T>` → `object` with `additionalProperties`
- `IEnumerable<T>` / arrays → `array` with `items`
- classes/structs/interfaces → `object` with `properties` and `required` (non-nullable value types are required)
- cycles handled via component-level caching and `OpenApiSchemaReference`

Property names honor `[JsonPropertyName]` and `[DataMember]`, falling back to camelCase.

Nullable types are wrapped in a `OneOf [ core, null ]` union (Microsoft.OpenApi 3.x has no `.Nullable` setter).

## Security schemes

A default Bearer JWT scheme is registered:

```csharp
new OpenApiSecurityScheme
{
    Type = SecuritySchemeType.Http,
    Scheme = "Bearer",
    BearerFormat = "JWT",
    Description = "Enter your JWT Bearer token."
}
```

Clear or replace `SecuritySchemes` to opt out. The provider serializes the document as OpenAPI 3.1 (`SerializeAsV31`).

## Source files

- `src/OpenApi/BdOpenApiDocumentBuilder.cs`
- `src/OpenApi/BdOpenApiSchemaGenerator.cs`
- `src/OpenApi/BdOpenApiOptions.cs`
- `src/OpenApi/OpenApiDocumentProvider.cs`
- `src/OpenApi/IOpenApiDocumentProvider.cs`
- `src/OpenApi/OpenApiEndpointRouteBuilderExtensions.cs`
- `src/OpenApi/ServiceAttributeHelper.cs`
- `src/OpenApi/ActionAttributeHelper.cs`
