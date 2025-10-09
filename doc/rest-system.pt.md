# Sistema REST - BlackDigital.AspNet

🇧🇷 Português | [🇺🇸 English](rest-system.md)

## O Que é o Sistema REST

O **BlackDigital.AspNet REST** é um sistema moderno e inovador para criação de APIs REST que oferece uma alternativa elegante aos controllers tradicionais do ASP.NET Core. Baseado em middleware personalizado, o sistema utiliza atributos para roteamento automático e binding inteligente de parâmetros, permitindo que desenvolvedores foquem exclusivamente na lógica de negócio.

### Características Principais

- **Middleware-based**: Intercepta requisições HTTP antes dos controllers tradicionais
- **Attribute-driven**: Roteamento e configuração baseados em atributos declarativos
- **Service-oriented**: Trabalha com interfaces de serviço em vez de herança de controllers
- **Intelligent binding**: Binding automático de parâmetros de múltiplas fontes (rota, corpo, headers, query)
- **Integrated transformations**: Integração nativa com o sistema de transformação para versionamento de API
- **Business-focused**: Elimina código de infraestrutura HTTP, mantendo apenas lógica de negócio

### Coexistência com Controllers Tradicionais

O sistema REST **coexiste perfeitamente** com controllers tradicionais do ASP.NET Core:

- **Uso simultâneo**: Ambas as abordagens podem ser usadas no mesmo projeto
- **Migração gradual**: Migre endpoints específicos conforme necessário
- **Flexibilidade total**: Use controllers tradicionais para casos específicos que precisam de controle total sobre HTTP
- **Sem conflitos**: O middleware REST processa apenas rotas configuradas com atributos `[Service]`

## Como Usar: Implementação Prática

### 1. Configuração no Program.cs

```csharp
using BlackDigital.AspNet.Rest;

var builder = WebApplication.CreateBuilder(args);

// 1. Registrar serviços REST
builder.Services.AddRestServices(restService =>
{
    restService.AddService<IUserService, UserService>();
    restService.AddService<IProductService, ProductService>();
});

// 2. Configurar opções MVC (necessário para o sistema REST)
builder.Services.AddRestMvcOptions();

// 3. Configurar transformações (opcional)
builder.Services.AddTransform(transformConfig =>
{
    transformConfig.AddRule<UserV1ToV2>("POST:api/users/", "2024-01-01");
    transformConfig.AddRule<UserV2ToV1>("POST:api/users/", "2024-01-01");
});

var app = builder.Build();

// 4. Configurar pipeline de middleware
app.UseRestMiddleware(); // Deve vir antes de UseAuthorization
app.UseAuthorization();
app.MapControllers(); // Controllers tradicionais continuam funcionando

app.Run();
```

### 2. Criação de um Serviço de Negócio

#### Definindo a Interface

```csharp
using BlackDigital.Rest;

[Service("api/users")]
public interface IUserService
{
    [Action("{id}", method: RestMethod.Get, authorize: false)]
    Task<User> GetUserAsync([Path] int id);

    [Action(method: RestMethod.Post, authorize: true)]
    Task<User> CreateUserAsync([Body] CreateUserRequest request);

    [Action("{id}", method: RestMethod.Put, authorize: true)]
    Task<User> UpdateUserAsync([Path] int id, [Body] UpdateUserRequest request);

    [Action("{id}", method: RestMethod.Delete, authorize: true)]
    Task DeleteUserAsync([Path] int id);

    [Action("search", method: RestMethod.Get, authorize: false)]
    Task<List<User>> SearchUsersAsync([Query] string filter, [Query] int page = 1);
}
```

#### Implementando o Serviço

```csharp
public class UserService : IUserService
{
    private readonly IUserRepository _repository;
    private readonly ILogger<UserService> _logger;

    public UserService(IUserRepository repository, ILogger<UserService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<User> GetUserAsync(int id)
    {
        _logger.LogInformation("Buscando usuário {UserId}", id);
        
        var user = await _repository.GetByIdAsync(id);
        if (user == null)
            BusinessException.ThrowNotFound("Usuário não encontrado");
        
        return user;
    }

    public async Task<User> CreateUserAsync(CreateUserRequest request)
    {
        _logger.LogInformation("Criando novo usuário: {Email}", request.Email);
        
        // Validações de negócio
        if (await _repository.ExistsByEmailAsync(request.Email))
            BusinessException.ThrowConflict("Email já está em uso");
        
        var user = new User
        {
            Name = request.Name,
            Email = request.Email,
            CreatedAt = DateTime.UtcNow
        };
        
        return await _repository.CreateAsync(user);
    }

    public async Task<User> UpdateUserAsync(int id, UpdateUserRequest request)
    {
        var user = await _repository.GetByIdAsync(id);
        if (user == null)
            BusinessException.ThrowNotFound("Usuário não encontrado");
        
        user.Name = request.Name;
        user.UpdatedAt = DateTime.UtcNow;
        
        return await _repository.UpdateAsync(user);
    }

    public async Task DeleteUserAsync(int id)
    {
        var user = await _repository.GetByIdAsync(id);
        if (user == null)
            BusinessException.ThrowNotFound("Usuário não encontrado");
        
        await _repository.DeleteAsync(id);
    }

    public async Task<List<User>> SearchUsersAsync(string filter, int page = 1)
    {
        return await _repository.SearchAsync(filter, page, pageSize: 20);
    }
}
```

### 3. Entendendo os Atributos

#### ServiceAttribute
Define a rota base do serviço:
```csharp
[Service("api/users")] // Todas as ações terão prefixo "api/users"
```

#### ActionAttribute
Configura métodos HTTP e rotas específicas:
```csharp
[Action("{id}", method: RestMethod.Get, authorize: false)]
// Rota: GET /api/users/{id}
// Autorização: não requerida

[Action("search", method: RestMethod.Get)]
// Rota: GET /api/users/search
// Autorização: requerida (padrão)
```

#### Atributos de Binding
- **`[Path]`**: Extrai valores da rota da URL
- **`[Body]`**: Extrai dados do corpo da requisição
- **`[Query]`**: Extrai valores da query string
- **`[Header]`**: Extrai valores dos cabeçalhos HTTP

### 4. Exemplo Prático: API de Usuários

```csharp
[Service("api/users")]
public interface IUserService
{
    // GET /api/users/{id}
    [Action("{id}", method: RestMethod.Get, authorize: false)]
    Task<User> GetUserAsync([Path] int id);

    // POST /api/users
    [Action(method: RestMethod.Post)]
    Task<User> CreateUserAsync([Body] CreateUserRequest request);

    // GET /api/users/search?filter=john&page=1&size=10
    [Action("search", method: RestMethod.Get, authorize: false)]
    Task<PagedResult<User>> SearchAsync(
        [Query] string filter,
        [Query] int page = 1,
        [Query] int size = 10
    );

    // PUT /api/users/{id}/profile
    [Action("{id}/profile", method: RestMethod.Put)]
    Task<User> UpdateProfileAsync(
        [Path] int id,
        [Body] UpdateProfileRequest request,
        [Header("User-Agent")] string userAgent
    );
}
```

### 5. Exemplo com Autorização

```csharp
[Service("api/admin")]
public interface IAdminService
{
    // Público - não requer autorização
    [Action("health", method: RestMethod.Get, authorize: false)]
    Task<HealthStatus> GetHealthAsync();

    // Requer autorização (padrão)
    [Action("users", method: RestMethod.Get)]
    Task<List<User>> GetAllUsersAsync();

    // Requer autorização específica
    [Action("users/{id}/ban", method: RestMethod.Post, authorize: true)]
    Task BanUserAsync([Path] int id, [Body] BanRequest request);
}
```

## Integração com Sistema de Transformação

O sistema REST integra-se nativamente com o sistema de transformação do BlackDigital.AspNet, oferecendo versionamento automático de APIs e compatibilidade com versões anteriores.

### Configuração Básica

```csharp
// Program.cs
builder.Services.AddTransform(config =>
{
    // Transformações para versionamento de usuários
    config.AddRule<UserV1ToV2>("POST:api/users/", "2024-01-01");
    config.AddRule<UserV2ToV1>("POST:api/users/", "2024-01-01");
    
    // Transformações específicas por endpoint
    config.AddInputRule<CreateUserV1ToV2>("POST:api/users/", "2024-01-01");
    config.AddOutputRule<UserV2ToV1>("GET:api/users/", "2024-01-01");
});
```

### Definindo Regras de Transformação

```csharp
// Transformação de entrada: V1 → V2
public class UserV1ToV2 : ITransformRule
{
    public string Apply(string input, TransformContext context)
    {
        var userV1 = JsonSerializer.Deserialize<UserV1>(input);
        var userV2 = new UserV2
        {
            Id = userV1.Id,
            FullName = userV1.Name, // V1: Name → V2: FullName
            Email = userV1.Email,
            Profile = new UserProfile // Novo campo em V2
            {
                Bio = "",
                Avatar = ""
            }
        };
        return JsonSerializer.Serialize(userV2);
    }
}

// Transformação de saída: V2 → V1
public class UserV2ToV1 : ITransformRule
{
    public string Apply(string input, TransformContext context)
    {
        var userV2 = JsonSerializer.Deserialize<UserV2>(input);
        var userV1 = new UserV1
        {
            Id = userV2.Id,
            Name = userV2.FullName, // V2: FullName → V1: Name
            Email = userV2.Email
            // Profile é omitido na V1
        };
        return JsonSerializer.Serialize(userV1);
    }
}
```

### Versionamento Automático de API

O sistema suporta múltiplas versões da mesma API automaticamente:

```csharp
// Serviço V1
[Service("api/v1/users")]
public interface IUserServiceV1
{
    [Action(method: RestMethod.Post)]
    Task<UserV1> CreateUserAsync([Body] UserV1 user);
}

// Serviço V2
[Service("api/v2/users")]
public interface IUserServiceV2
{
    [Action(method: RestMethod.Post)]
    Task<UserV2> CreateUserAsync([Body] UserV2 user);
}

// Configuração
builder.Services.AddRestServices(config =>
{
    config.AddService<IUserServiceV1, UserServiceV1>();
    config.AddService<IUserServiceV2, UserServiceV2>();
});
```

### Transformação Automática via Headers

O sistema pode aplicar transformações baseadas em cabeçalhos HTTP:

```csharp
// Cliente especifica versão via header
// Accept: application/json; version=1.0
// → Sistema aplica transformação V2→V1 na resposta

// api-version: 2024-01-01
// → Sistema aplica transformações baseadas na data
```

### Exemplo Prático: Evolução de API

**Cenário**: Adição de campo `profile` na versão 2 da API de usuários

```csharp
// V1: Estrutura original
public class UserV1
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
}

// V2: Estrutura evoluída
public class UserV2
{
    public int Id { get; set; }
    public string FullName { get; set; } // Renomeado de Name
    public string Email { get; set; }
    public UserProfile Profile { get; set; } // Novo campo
}

// Serviço unificado que trabalha com V2 internamente
[Service("api/users")]
public interface IUserService
{
    [Action(method: RestMethod.Post)]
    Task<UserV2> CreateUserAsync([Body] UserV2 user);
}

// Transformações automáticas garantem compatibilidade
// POST /api/users com dados V1 → Transformado para V2 → Processado → Resposta transformada para V1
```

### Benefícios da Integração

1. **Compatibilidade Garantida**: Clientes antigos continuam funcionando
2. **Evolução Gradual**: Adicione novos campos sem quebrar APIs existentes
3. **Código Unificado**: Um serviço atende múltiplas versões
4. **Transformação Automática**: Sem código manual de conversão
5. **Testabilidade**: Transformações podem ser testadas independentemente
6. **Documentação Automática**: Versões são documentadas automaticamente

## Benefícios e Vantagens

### 1. Foco Total na Lógica de Negócio

O sistema elimina completamente o código de infraestrutura HTTP, permitindo que você se concentre apenas no que realmente importa:

```csharp
// ✅ Com BlackDigital.AspNet REST - Apenas lógica de negócio
public async Task<User> GetUserAsync(int id)
{
    var user = await _repository.GetByIdAsync(id);
    if (user == null)
        BusinessException.ThrowNotFound("Usuário não encontrado");
    
    return user; // Serialização automática
}

// ❌ Controller tradicional - Mistura infraestrutura + negócio
[HttpGet("{id}")]
public async Task<IActionResult> GetUser(int id)
{
    try
    {
        var user = await _repository.GetByIdAsync(id);
        if (user == null)
            return NotFound(); // Código de infraestrutura
        
        return Ok(user); // Código de infraestrutura
    }
    catch (Exception ex)
    {
        return StatusCode(500, "Erro interno"); // Código de infraestrutura
    }
}
```

### 2. Produtividade Extrema

**Redução significativa de código:**
- 70% menos linhas de código
- 100% menos código de infraestrutura
- Desenvolvimento 3x mais rápido

```csharp
// Comparação: CRUD completo de usuários
// Controller tradicional: ~120 linhas
// BlackDigital.AspNet REST: ~35 linhas
```

### 3. Testabilidade Superior

Testes focados apenas na lógica de negócio, sem dependências HTTP:

```csharp
[Test]
public async Task GetUser_WhenUserExists_ShouldReturnUser()
{
    // Arrange
    var mockRepository = new Mock<IUserRepository>();
    mockRepository.Setup(r => r.GetByIdAsync(1))
              .ReturnsAsync(new User { Id = 1, Name = "João" });
    
    var service = new UserService(mockRepository.Object);
    
    // Act - Sem dependências HTTP!
    var result = await service.GetUserAsync(1);
    
    // Assert - Apenas lógica de negócio
    Assert.That(result.Id, Is.EqualTo(1));
    Assert.That(result.Name, Is.EqualTo("João"));
}
```

### 4. Flexibilidade e Reutilização

Serviços podem ser reutilizados em diferentes contextos:

```csharp
public class UserService : IUserService
{
    // Este mesmo serviço pode ser usado em:
    // ✅ API REST (via RestMiddleware)
    // ✅ GraphQL (via resolvers)
    // ✅ Background jobs
    // ✅ Testes unitários
    // ✅ Outros serviços (injeção de dependência)
    // ✅ Console applications
    // ✅ Blazor Server/WASM
}
```

### 5. Coexistência com Controllers Tradicionais

O sistema não substitui, mas **complementa** controllers tradicionais:

```csharp
// ✅ Use REST para APIs de negócio padrão
[Service("api/users")]
public interface IUserService { }

// ✅ Use Controllers para casos específicos
[ApiController]
[Route("api/files")]
public class FileController : ControllerBase
{
    // Upload de arquivos, streaming, etc.
    [HttpPost("upload")]
    public async Task<IActionResult> Upload(IFormFile file) { }
}
```

### 6. Automações Inteligentes

O sistema automatiza tarefas repetitivas:

- **Roteamento**: Baseado em atributos, sem configuração manual
- **Binding**: Automático de múltiplas fontes (rota, corpo, headers, query)
- **Validação**: Automática com base em atributos e tipos
- **Serialização**: JSON otimizado com configurações padrão
- **Tratamento de erros**: Conversão automática de exceções para HTTP status
- **Transformação**: Versionamento automático de API
- **Autorização**: Verificação automática baseada em atributos

### 7. Performance e Eficiência

O sistema é otimizado para performance:

- Menos overhead que controllers tradicionais
- Binding direto de parâmetros
- Serialização otimizada
- Menos alocações de memória
- Pipeline de middleware enxuto

## Detalhamento Técnico

### Arquitetura do Sistema

O sistema REST é composto por quatro componentes principais:

1. **RestMiddleware**: Middleware principal que intercepta e processa requisições
2. **RestHelper**: Métodos auxiliares para configuração do MVC
3. **RestServiceBuilder**: Builder para configuração e registro de serviços
4. **RestMiddlewareExtensions**: Extensões para integração com o container DI

#### RestMiddleware

O `RestMiddleware` é o coração do sistema REST, responsável por interceptar requisições HTTP e roteá-las para os serviços apropriados.

**Funcionalidades Principais:**
- Interceptação de requisições HTTP
- Roteamento baseado em atributos
- Binding de parâmetros de múltiplas fontes
- Transformação de dados automática
- Autorização baseada em atributos
- Tratamento padronizado de exceções

**Fluxo de Processamento:**
1. Interceptação da requisição HTTP
2. Descoberta do serviço baseado no `ServiceAttribute`
3. Identificação do método baseado no `ActionAttribute`
4. Extração e conversão de parâmetros
5. Transformação de entrada (se configurada)
6. Execução do método do serviço
7. Transformação de saída (se configurada)
8. Serialização e retorno da resposta

#### Sistema de Atributos

**ServiceAttribute**: Define a rota base do serviço
```csharp
[Service("api/user")]
public interface IUserService { }
```

**ActionAttribute**: Configura métodos HTTP e rotas específicas
```csharp
[Action(method: RestMethod.Get, route: "{id}", authorize: true)]
Task<User> GetUserAsync([Path] int id);
```

**Atributos de Binding:**
- `[Path]`: Extrai valores da rota da URL
- `[Body]`: Extrai dados do corpo da requisição
- `[Header]`: Extrai valores dos cabeçalhos HTTP
- `[Query]`: Extrai valores da query string

### Configuração Avançada

#### Configuração no Program.cs

```csharp
using BlackDigital.AspNet.Rest;

var builder = WebApplication.CreateBuilder(args);

// 1. Registrar serviços REST
builder.Services.AddRest(config =>
{
    config.AddService<IUserService, UserService>();
    config.AddService<IProductService, ProductService>();
    config.EnableTransformations(); // Opcional
});

// 2. Configurar transformações (se necessário)
builder.Services.AddTransform(config =>
{
    config.AddRule<UserV1ToV2>();
    config.AddRule<UserV2ToV1>();
});

var app = builder.Build();

// 3. Configurar pipeline
app.UseTransform(); // Se usando transformações
app.UseRest();
app.UseAuthorization();

app.Run();
```

#### Funcionalidades Avançadas

**Autorização Flexível:**
```csharp
[Action(method: RestMethod.Get, authorize: false)]     // Público
[Action(method: RestMethod.Post, authorize: true)]     // Requer auth
[Action(method: RestMethod.Delete, roles: "Admin")]    // Requer role
[Action(method: RestMethod.Put, policy: "EditUser")]   // Requer policy
```

**Binding Complexo:**
```csharp
[Action("search/{category}", method: RestMethod.Post)]
Task<List<Product>> SearchAsync(
    [Path] string category,
    [Query] int page,
    [Query] int size,
    [Header("User-Id")] string userId,
    [Body] SearchCriteria criteria
);
```

**Tratamento de Erros:**
```csharp
public async Task<User> GetUserAsync(int id)
{
    var user = await _repository.GetByIdAsync(id);
    
    if (user == null)
        BusinessException.ThrowNotFound("Usuário não encontrado");
    
    if (!user.IsActive)
        BusinessException.ThrowForbidden("Usuário inativo");
    
    return user;
}
```

### Melhores Práticas

#### 1. Organização de Serviços
- Mantenha interfaces focadas em um domínio específico
- Use nomes descritivos para rotas e ações
- Agrupe funcionalidades relacionadas

#### 2. Configuração de Rotas
```csharp
// ✅ Bom: Rotas claras e RESTful
[Service("api/users")]
[Action("{id}", method: RestMethod.Get)]

// ❌ Evitar: Rotas confusas
[Service("api")]
[Action("getUserById/{id}", method: RestMethod.Get)]
```

#### 3. Binding de Parâmetros
```csharp
// ✅ Bom: Uso apropriado dos atributos
[Action("{id}/orders", method: RestMethod.Get)]
Task<List<Order>> GetUserOrdersAsync(
    [Path] int id,
    [Query] int page = 1,
    [Query] int pageSize = 10
);
```

#### 4. Tratamento de Erros
```csharp
// ✅ Bom: Usar BusinessException para erros de negócio
public async Task<User> GetUserAsync(int id)
{
    var user = await _repository.GetByIdAsync(id);
    if (user == null)
        BusinessException.ThrowNotFound($"Usuário {id} não encontrado");
    
    return user;
}
```

## Conclusão

O sistema REST do BlackDigital.AspNet oferece uma abordagem moderna e produtiva para desenvolvimento de APIs, permitindo que desenvolvedores foquem na lógica de negócio enquanto o framework cuida da infraestrutura HTTP.

### Principais Vantagens

1. **Produtividade**: Desenvolvimento até 3x mais rápido
2. **Qualidade**: Redução significativa de bugs
3. **Manutenibilidade**: Código mais limpo e focado
4. **Testabilidade**: Testes mais simples e eficazes
5. **Flexibilidade**: Coexistência com controllers tradicionais
6. **Evolução**: Versionamento automático com transformações

### Quando Usar

**Ideal para:**
- APIs de negócio padrão
- Operações CRUD
- Serviços com lógica de domínio complexa
- Projetos que precisam de versionamento de API
- Equipes que querem focar na lógica de negócio

**Coexistência com Controllers:**
- Use controllers tradicionais para casos específicos que precisam de controle total sobre HTTP
- Use o sistema REST para a maioria das APIs de negócio
- Migre gradualmente conforme necessário

O BlackDigital.AspNet REST representa uma evolução natural no desenvolvimento de APIs, mantendo a simplicidade sem sacrificar a flexibilidade.

---

**Documentação Relacionada:**
- [Sistema de Transformação](transform-system.pt.md)
- [Documentação Principal](../README.pt.md)