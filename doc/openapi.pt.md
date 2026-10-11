# OpenAPI + Scalar

🇧🇷 Português | [🇺🇸 English](openapi.md)

O módulo OpenAPI gera um documento OpenAPI 3.0 completo diretamente a partir das suas interfaces de serviço REST e expõe tanto o documento JSON bruto quanto uma UI interativa do **Scalar**. Ele constrói o documento com `Microsoft.OpenApi` 3.x — sem necessidade de Swashbuckle.

## Configuração

### Registrar serviços e middleware

```csharp
using BlackDigital.AspNet.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi(options =>
{
    options.Title = "Minha API";
    options.Version = "v1";
    options.Description = "API REST construída com BlackDigital.AspNet";

    // Habilite o documento/UI apenas nos ambientes de dev e stage
    options.Enabled = builder.Environment.IsDevelopment() || builder.Environment.IsStaging();

    options.Contact = new OpenApiContact
    {
        Name = "Suporte",
        Email = "suporte@exemplo.com"
    };
});

var app = builder.Build();

app.UseRouting();
app.UseRestMiddleware();
app.UseAuthorization();
app.MapControllers();
app.MapOpenApi();   // expõe /openapi/v1.json e a UI do Scalar

app.Run();
```

`MapOpenApi()` mapeia dois endpoints:
- O documento JSON OpenAPI em `options.OpenApiRoutePattern` (padrão `"/openapi/{documentName}.json"`)
- A UI interativa do Scalar em `"/scalar/v1"` (a rota pode ser customizada via `options.OpenApiRoutePattern`)

### BdOpenApiOptions

| Propriedade | Tipo | Padrão | Descrição |
|----------|------|---------|-------------|
| `Title` | `string` | `"BlackDigital API"` | Título do documento |
| `Version` | `string` | `"v1"` | Versão do documento |
| `Description` | `string?` | `null` | Descrição do documento |
| `Contact` | `OpenApiContact?` | `null` | Informações de contato da API |
| `License` | `OpenApiLicense?` | `null` | Informações de licença da API |
| `ServerUrl` | `string?` | `null` | URL base do servidor adicionada ao documento |
| `DocumentName` | `string` | `"v1"` | Nome usado na rota do JSON OpenAPI |
| `Enabled` | `bool` | `true` | Quando `false`, o documento não é gerado e as rotas não são mapeadas |
| `IncludedAssemblyPrefixes` | `string[]` | `{ "BlackDigital" }` | Assemblies cujos nomes começam com qualquer um destes são escaneados por interfaces de serviço |
| `ExcludedAssemblyPrefixes` | `string[]` | `{ "BlackDigital.AspNet", "BlackDigital.Rest" }` | Assemblies cujos nomes começam com qualquer um destes são ignorados |
| `SecuritySchemes` | `Dictionary<string, OpenApiSecurityScheme>` | Esquema Bearer JWT | Esquemas de segurança disponíveis no documento |
| `ApplyBearerAsGlobalSecurityRequirement` | `bool` | `true` | Aplica o esquema bearer como requisito de segurança global |
| `PreferredSecuritySchemes` | `string[]` | `{ "Bearer" }` | Esquemas de segurança aplicados às operações que exigem autorização |
| `ScalarTitle` | `string` | `"BlackDigital API"` | Título exibido na UI do Scalar |
| `OpenApiRoutePattern` | `string` | `"/openapi/{documentName}.json"` | Padrão de rota para o documento JSON |

## Como a Descoberta Funciona

1. Na primeira requisição, `OpenApiDocumentProvider.Build()` escaneia os assemblies carregados.
2. Para cada assembly cujo nome corresponde a `IncludedAssemblyPrefixes` (e não corresponde a `ExcludedAssemblyPrefixes`), ele enumera os tipos públicos.
3. Interfaces decoradas com `[Service]` (de `BlackDigital.Rest`) viram tags da API.
4. Métodos decorados com `[Action]` viram operações, com seus parâmetros `[Path]`, `[Body]`, `[Query]` e `[Header]` mapeados para parâmetros e corpos de requisição do OpenAPI.
5. O tipo de retorno vira o schema de resposta para `200 OK`.
6. Métodos/operações marcados com `authorize: true` recebem os esquemas de segurança preferidos.

## Nomeação de Tags e Operações

- **Tag**: o nome da interface com o `I` inicial removido (ex.: `IUserService` → `UserService`).
- **Resumo da operação**: o nome do método, com o `Async` final removido e espaços inseridos antes de letras maiúsculas (ex.: `GetUserAsync` → `Get User`).

## Geração de Schemas

O `BdOpenApiSchemaGenerator` produz schemas via reflexão:

- Enums são gerados como `string` enum do OpenAPI com os valores `enum`.
- Tipos complexos são gerados como `object` com `properties` e uma lista `required`.
- Tipos comuns do BCL mapeiam para seus equivalentes JSON Schema (`int` → `integer/int32`, `string` → `string`, `bool` → `boolean`, `DateTime` → `string/date-time`, etc.).
- `IEnumerable<T>` mapeia para `array` de `T`.
- `IDictionary<string, T>` mapeia para `object` com `additionalProperties` de `T`.
- `Nullable<T>` e tipos de referência nullable são marcados como nullable quando aplicável.
- `BlackDigital.Model.Id` é gerado como `string`.

Schemas são cacheados por `Type` para evitar reflexão repetida.

## Segurança Bearer JWT (Padrão)

Por padrão, um esquema de segurança Bearer chamado `Bearer` é registrado e aplicado globalmente:

```csharp
new OpenApiSecurityScheme
{
    Name = "Authorization",
    Type = SecuritySchemeType.Http,
    Scheme = "Bearer",
    BearerFormat = "JWT",
    In = ParameterLocation.Header,
    Description = "Informe o token JWT obtido no endpoint de autenticação."
}
```

Para adicionar seus próprios esquemas ou desabilitar o requisito global, configure `SecuritySchemes` e `ApplyBearerAsGlobalSecurityRequirement` em `AddOpenApi`.

## Desabilitando o Documento em Produção

Como `Enabled` tem padrão `true`, recomenda-se condicioná-lo ao ambiente:

```csharp
builder.Services.AddOpenApi(options =>
{
    options.Enabled = builder.Environment.IsDevelopment() || builder.Environment.IsStaging();
});
```

Quando `Enabled` é `false`, `MapOpenApi()` não registra nenhuma rota e `OpenApiDocumentProvider.GetOpenApiDocument()` retorna `null`.

## Documentação Relacionada

- [Sistema REST](rest-system.pt.md)
- [Documentação Principal](../README.pt.md)
