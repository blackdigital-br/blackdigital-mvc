# BlackDigital.AspNet

🇧🇷 Português | [🇺🇸 English](README.md)

<div align="center">
  <img src="https://raw.githubusercontent.com/blackdigital-br/blackdigital-mvc/main/doc/images/Logo128.png" alt="BlackDigital.AspNet Logo" width="128" height="128">
  
  **Framework moderno para desenvolvimento de APIs REST em .NET**
  
  [![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
  [![.NET](https://img.shields.io/badge/.NET-10.0%2B-purple.svg)](https://dotnet.microsoft.com/)
</div>

## 📋 Sobre o Projeto

O **BlackDigital.AspNet** é um framework inovador que simplifica drasticamente o desenvolvimento de APIs REST em .NET, eliminando a necessidade de controllers tradicionais e oferecendo um sistema avançado de transformação para evolução de APIs com compatibilidade total.

### 🚀 Principais Funcionalidades

- **🔧 Sistema REST**: Criação de APIs sem controllers usando interfaces de serviço e atributos
- **🔄 Sistema de Transformação**: Evolução de API mantendo compatibilidade com versões anteriores
- **📖 OpenAPI + Scalar**: Geração automática de documentação da API (sem Swashbuckle)
- **🔐 Segurança**: Serviço de tokens JWT e hash de senhas Argon2id
- **📧 Infraestrutura de Email**: Serviço de email SMTP com suporte a templates HTML
- **⚡ Alta Performance**: Middleware otimizado para máxima eficiência
- **🧪 Testabilidade**: Arquitetura que facilita testes unitários e de integração
- **📦 Modular**: Componentes independentes e reutilizáveis

### 💡 Por que usar o BlackDigital.AspNet?

- **Produtividade**: Reduza até 70% do código boilerplate
- **Manutenibilidade**: Código mais limpo e organizado
- **Evolução Segura**: Atualize APIs sem quebrar clientes existentes
- **Flexibilidade**: Coexiste perfeitamente com controllers tradicionais
- **Simplicidade**: Foco na lógica de negócio, não na infraestrutura

## 📦 Instalação

### Via NuGet Package Manager
```bash
Install-Package BlackDigital.AspNet
```

### Via .NET CLI
```bash
dotnet add package BlackDigital.AspNet
```

### Via PackageReference
```xml
<PackageReference Include="BlackDigital.AspNet" Version="25.10.7.16" />
```

## ⚙️ Configuração Básica

### 1. Configure no Program.cs

```csharp
using BlackDigital.AspNet.OpenApi;
using BlackDigital.AspNet.Rest;
using BlackDigital.AspNet.Rest.Transforms;

var builder = WebApplication.CreateBuilder(args);

// Registrar serviços REST (interface + implementação)
builder.Services.AddRestServices(rest =>
{
    rest.AddService<IUserService, UserService>();
});

// Configurar opções do MVC (model binders, route constraints, filtro de validação)
builder.Services.AddRestMvcOptions();

// Configurar o sistema de transformação (opcional)
builder.Services.AddTransform(transform =>
{
    transform.AddInputRule<OldUserToUserRule>("POST:api/users/", "2020-01-01");
});

// Configurar documentação OpenAPI + Scalar (opcional)
builder.Services.AddOpenApi(options =>
{
    options.Title = "Minha API";
    options.Version = "v1";
    options.Enabled = builder.Environment.IsDevelopment() || builder.Environment.IsStaging();
});

var app = builder.Build();

app.UseRouting();
app.UseRestMiddleware();   // Middleware REST (antes de UseAuthorization)
app.UseAuthorization();
app.MapControllers();      // Controllers tradicionais continuam funcionando
app.MapOpenApi();          // JSON OpenAPI + UI do Scalar

app.Run();
```

### 2. Crie seu primeiro serviço

Serviços são definidos como **interfaces** decoradas com `[Service]`, com métodos decorados com `[Action]`:

```csharp
using BlackDigital.Rest;

[Service("api/users")]
public interface IUserService
{
    [Action("{id}", method: RestMethod.Get, authorize: false)]
    Task<User> GetUserAsync([Path] int id);

    [Action(method: RestMethod.Post, authorize: true)]
    Task<User> CreateUserAsync([Body] CreateUserRequest request);
}
```

Implemente a interface como uma classe comum (não é necessário herdar de controller):

```csharp
public class UserService : IUserService
{
    public async Task<User> GetUserAsync(int id)
    {
        // Sua lógica aqui
        return await GetUserByIdAsync(id);
    }

    public async Task<User> CreateUserAsync(CreateUserRequest request)
    {
        // Sua lógica aqui
        return await CreateUserAsync(request);
    }
}
```

## 🎯 Principais Recursos

### 🔧 Sistema REST
Crie APIs REST completas sem controllers tradicionais:
- **Baseado em interfaces**: Atributos `[Service]` e `[Action]` definem rotas automaticamente
- **Binding Automático**: Parâmetros `[Path]`, `[Body]`, `[Query]`, `[Header]` vinculados automaticamente
- **Suporte nativo a `Id`**: Model binder e route constraint customizados para `BlackDigital.Model.Id`
- **CancellationToken**: Token de cancelamento da requisição é vinculado automaticamente
- **Autorização**: Flag `authorize` por ação ou por serviço
- **Business exceptions**: `BusinessException` mapeia diretamente para códigos de status HTTP
- **Suporte Completo**: GET, POST, PUT, DELETE, PATCH, HEAD, OPTIONS
- **Async/Await**: Suporte nativo a `Task`, `Task<T>`, `ValueTask`, `ValueTask<T>`

### 🔄 Sistema de Transformação
Evolua suas APIs mantendo compatibilidade:
- **Regras de entrada e saída**: Transforme corpo da requisição e resposta independentemente
- **Versionamento por data**: Versão selecionada via query string `api-version` ou header `x-api-version`
- **Encadeamento automático**: Regras são aplicadas sequencialmente por ordem de versão
- **Integração com DI**: Tipos de regra são instanciados através do container de DI

### 📖 OpenAPI + Scalar
Documentação automática sem Swashbuckle:
- **Microsoft.OpenApi puro**: Construído diretamente com `Microsoft.OpenApi` 3.x
- **Descoberta automática**: Escaneia assemblies em busca de interfaces com `[Service]`
- **UI do Scalar**: Documentação interativa via `Scalar.AspNetCore`
- **Bearer JWT**: Esquema de segurança pré-configurado (sobrescrevível)
- **Customizável**: Título, versão, contato, licença, URL do servidor, filtros de assembly

### 🔐 Segurança
Blocos de segurança prontos para uso:
- **Serviço JWT**: Gere, valide e inspecione tokens JWT
- **Argon2id**: Hash de senhas com memória/iterações/paralelismo configuráveis

### 📧 Infraestrutura de Email
Envie emails transacionais com facilidade:
- **SMTP via MailKit**: Suporte a múltiplos provedores com configuração de fallback
- **Templates HTML**: Carregue templates de `templates/email/` com placeholders `{{ chave }}`
- **API Fluente**: `Email.Create().To(...).WithSubject(...).WithBody(...)`

## 📚 Documentação Detalhada

### 📖 Guias Completos
- **[Sistema REST](doc/rest-system.pt.md)** - Guia completo do sistema REST
- **[Sistema de Transformação](doc/transform-system.pt.md)** - Guia completo do sistema de transformação
- **[OpenAPI + Scalar](doc/openapi.pt.md)** - Guia de geração de documentação da API
- **[Segurança](doc/security.pt.md)** - Guia de JWT e hash de senhas
- **[Email](doc/email.pt.md)** - Guia de infraestrutura de email

### 🎯 Navegação Rápida
- [Como criar serviços REST](doc/rest-system.pt.md#como-usar-implementação-prática)
- [Configuração de transformações](doc/transform-system.pt.md#configuração-e-uso)
- [Configuração do OpenAPI](doc/openapi.pt.md#configuração)
- [Configuração do serviço JWT](doc/security.pt.md#serviço-de-token-jwt)
- [Configuração do serviço de email](doc/email.pt.md#configuração)
- [Melhores práticas](doc/rest-system.pt.md#melhores-práticas)

## 🚀 Exemplos Rápidos

### Serviço REST Simples
```csharp
[Service("api/products")]
public interface IProductService
{
    [Action(method: RestMethod.Get, authorize: false)]
    Task<IEnumerable<Product>> GetProductsAsync();

    [Action("{id}", method: RestMethod.Get, authorize: false)]
    Task<Product> GetProductAsync([Path] int id);

    [Action(method: RestMethod.Post)]
    Task<Product> CreateProductAsync([Body] Product product);
}
```

### Transformação Básica
```csharp
public class OldUserToUserRule : TransformRule<OldUser, User>
{
    public override User? Transform(OldUser? source)
    {
        if (source == null) return null;
        return new User
        {
            Id = source.Id,
            FullName = $"{source.FirstName} {source.LastName}",
            Email = source.Email
        };
    }
}
```

### 📁 Projeto de Exemplo Completo
Explore o [projeto de exemplo](example/) que demonstra:
- Configuração completa
- Serviços REST (baseados em interface)
- Transformações de entrada e saída
- Configuração do OpenAPI + Scalar

## 📁 Estrutura do Projeto

```
BlackDigital.AspNet/
├── 📁 src/                    # Código fonte principal
│   ├── 📁 Rest/              # Sistema REST (middleware, roteamento, binding)
│   │   └── 📁 Transforms/    # Motor de transformação
│   ├── 📁 OpenApi/           # Construtor de documento OpenAPI + integração Scalar
│   ├── 📁 Security/          # Serviços JWT e Argon2id
│   ├── 📁 Infrastructures/   # Serviços de email (SMTP + templates)
│   ├── 📁 Binder/            # Model binders customizados (Id)
│   ├── 📁 Constraint/        # Route constraints (id)
│   ├── 📁 Filters/           # Filtros MVC (validação de ModelState)
│   └── 📁 Services/          # Helpers de DI e utilitários de HTTP context
├── 📁 example/               # Projeto de exemplo
├── 📁 test/                  # Testes unitários
├── 📁 doc/                   # Documentação
│   ├── 📄 rest-system.pt.md  # Documentação do sistema REST
│   ├── 📄 transform-system.pt.md # Documentação de transformações
│   ├── 📄 openapi.pt.md      # Documentação do OpenAPI + Scalar
│   ├── 📄 security.pt.md     # Documentação de segurança (JWT / Argon2id)
│   ├── 📄 email.pt.md        # Documentação de infraestrutura de email
│   └── 📁 images/            # Imagens da documentação
└── 📄 README.md              # Este arquivo
```

### 🔍 Onde Encontrar
- **Exemplos**: Pasta `example/` contém implementação completa
- **Testes**: Pasta `test/` com exemplos de uso e testes
- **Documentação**: Pasta `doc/` com guias detalhados
- **Código Fonte**: Pasta `src/` com implementação

## 🤝 Contribuindo

Contribuições são bem-vindas! Para contribuir:

1. **Fork** o projeto
2. **Crie** uma branch para sua feature (`git checkout -b feature/AmazingFeature`)
3. **Commit** suas mudanças (`git commit -m 'Add some AmazingFeature'`)
4. **Push** para a branch (`git push origin feature/AmazingFeature`)
5. **Abra** um Pull Request

### 📋 Diretrizes
- Siga os padrões de código existentes
- Adicione testes para novas funcionalidades
- Atualize a documentação quando necessário
- Use português brasileiro para documentação

## 📄 Licença

Este projeto está licenciado sob a Licença MIT - veja o arquivo [LICENSE](LICENSE) para detalhes.

## 🆘 Suporte

### 📞 Canais de Suporte
- **Issues**: [GitHub Issues](https://github.com/blackdigital-br/blackdigital-mvc/issues)
- **Discussões**: [GitHub Discussions](https://github.com/blackdigital-br/blackdigital-mvc/discussions)
- **Email**: suporte@blackdigital.com.br

### 📚 Recursos Adicionais
- [Documentação Completa](doc/)
- [Exemplos Práticos](example/)

---

<div align="center">
  <p>Feito com ❤️ pela equipe <strong>BlackDigital</strong></p>
  <p>
    <a href="https://blackdigital.com.br">Website</a> •
    <a href="doc/">Documentação</a> •
    <a href="example/">Exemplos</a> •
    <a href="https://github.com/blackdigital-br/blackdigital-mvc/issues">Suporte</a>
  </p>
</div>
