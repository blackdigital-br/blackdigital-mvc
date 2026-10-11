# OpenAPI + Scalar

[🇧🇷 Português](openapi.pt.md) | 🇺🇸 English

The OpenAPI module generates a complete OpenAPI 3.0 document directly from your REST service interfaces and exposes both the raw JSON document and an interactive **Scalar** UI. It builds the document with `Microsoft.OpenApi` 3.x — no Swashbuckle required.

## Configuration

### Register services and middleware

```csharp
using BlackDigital.AspNet.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi(options =>
{
    options.Title = "My API";
    options.Version = "v1";
    options.Description = "REST API built with BlackDigital.AspNet";

    // Only enable the document/UI in dev and stage environments
    options.Enabled = builder.Environment.IsDevelopment() || builder.Environment.IsStaging();

    options.Contact = new OpenApiContact
    {
        Name = "Support",
        Email = "support@example.com"
    };
});

var app = builder.Build();

app.UseRouting();
app.UseRestMiddleware();
app.UseAuthorization();
app.MapControllers();
app.MapOpenApi();   // exposes /openapi/v1.json and the Scalar UI

app.Run();
```

`MapOpenApi()` maps two endpoints:
- The OpenAPI JSON document at `options.OpenApiRoutePattern` (default `"/openapi/{documentName}.json"`)
- The Scalar interactive UI at `"/scalar/v1"` (route can be customised via `options.OpenApiRoutePattern`)

### BdOpenApiOptions

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Title` | `string` | `"BlackDigital API"` | Document title |
| `Version` | `string` | `"v1"` | Document version |
| `Description` | `string?` | `null` | Document description |
| `Contact` | `OpenApiContact?` | `null` | API contact information |
| `License` | `OpenApiLicense?` | `null` | API license information |
| `ServerUrl` | `string?` | `null` | Base server URL added to the document |
| `DocumentName` | `string` | `"v1"` | Name used in the OpenAPI JSON route |
| `Enabled` | `bool` | `true` | When `false`, the document is not generated and routes are not mapped |
| `IncludedAssemblyPrefixes` | `string[]` | `{ "BlackDigital" }` | Assemblies whose names start with any of these are scanned for service interfaces |
| `ExcludedAssemblyPrefixes` | `string[]` | `{ "BlackDigital.AspNet", "BlackDigital.Rest" }` | Assemblies whose names start with any of these are skipped |
| `SecuritySchemes` | `Dictionary<string, OpenApiSecurityScheme>` | Bearer JWT scheme | Security schemes available in the document |
| `ApplyBearerAsGlobalSecurityRequirement` | `bool` | `true` | Applies the bearer scheme as a global security requirement |
| `PreferredSecuritySchemes` | `string[]` | `{ "Bearer" }` | Security schemes applied to operations that require authorization |
| `ScalarTitle` | `string` | `"BlackDigital API"` | Title shown in the Scalar UI |
| `OpenApiRoutePattern` | `string` | `"/openapi/{documentName}.json"` | Route pattern for the JSON document |

## How Discovery Works

1. On first request, `OpenApiDocumentProvider.Build()` scans loaded assemblies.
2. For each assembly whose name matches `IncludedAssemblyPrefixes` (and does not match `ExcludedAssemblyPrefixes`), it enumerates public types.
3. Interfaces decorated with `[Service]` (from `BlackDigital.Rest`) become API tags.
4. Methods decorated with `[Action]` become operations, with their `[Path]`, `[Body]`, `[Query]`, and `[Header]` parameters mapped to OpenAPI parameters and request bodies.
5. The return type becomes the response schema for `200 OK`.
6. Methods/operations flagged with `authorize: true` get the preferred security schemes applied.

## Tag and Operation Naming

- **Tag**: the interface name with the leading `I` stripped (e.g. `IUserService` → `UserService`).
- **Operation summary**: the method name, with the trailing `Async` removed and spaces inserted before uppercase letters (e.g. `GetUserAsync` → `Get User`).

## Schema Generation

`BdOpenApiSchemaGenerator` produces schemas via reflection:

- Enums are generated as OpenAPI `string` enums with the `enum` values.
- Complex types are generated as `object` with `properties` and a `required` list.
- Common BCL types map to their JSON Schema equivalents (`int` → `integer/int32`, `string` → `string`, `bool` → `boolean`, `DateTime` → `string/date-time`, etc.).
- `IEnumerable<T>` maps to `array` of `T`.
- `IDictionary<string, T>` maps to `object` with `additionalProperties` of `T`.
- `Nullable<T>` and nullable reference types are marked nullable where applicable.
- `BlackDigital.Model.Id` is generated as `string`.

Schemas are cached per `Type` to avoid repeated reflection.

## Bearer JWT Security (Default)

By default, a Bearer security scheme named `Bearer` is registered and applied globally:

```csharp
new OpenApiSecurityScheme
{
    Name = "Authorization",
    Type = SecuritySchemeType.Http,
    Scheme = "Bearer",
    BearerFormat = "JWT",
    In = ParameterLocation.Header,
    Description = "Enter the JWT token obtained from the authentication endpoint."
}
```

To add your own schemes or disable the global requirement, configure `SecuritySchemes` and `ApplyBearerAsGlobalSecurityRequirement` in `AddOpenApi`.

## Disabling the Document in Production

Because `Enabled` defaults to `true`, it is recommended to gate it by environment:

```csharp
builder.Services.AddOpenApi(options =>
{
    options.Enabled = builder.Environment.IsDevelopment() || builder.Environment.IsStaging();
});
```

When `Enabled` is `false`, `MapOpenApi()` does not register any routes, and `OpenApiDocumentProvider.GetOpenApiDocument()` returns `null`.

## Related Documentation

- [REST System](rest-system.md)
- [Main Documentation](../README.md)
