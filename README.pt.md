# BlackDigital.AspNet

🇧🇷 Português | [🇺🇸 English](README.md)

<div align="center">
  <img src="doc/images/Logo128.png" alt="BlackDigital.AspNet Logo" width="128" height="128">
  
  **Framework moderno para desenvolvimento de APIs REST em .NET**
  
  [![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
  [![.NET](https://img.shields.io/badge/.NET-10.0%2B-purple.svg)](https://dotnet.microsoft.com/)
</div>

## 📋 Sobre o Projeto

O **BlackDigital.AspNet** é um framework inovador que simplifica drasticamente o desenvolvimento de APIs REST em .NET, eliminando a necessidade de controllers tradicionais e oferecendo um sistema avançado de transformação para evolução de APIs com compatibilidade total.

### 🚀 Principais Funcionalidades

- **🔧 Sistema REST**: Criação de APIs sem controllers usando apenas atributos
- **🔄 Sistema de Transformação**: Evolução de API mantendo compatibilidade com versões anteriores
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
using BlackDigital.AspNet;

var builder = WebApplication.CreateBuilder(args);

// Adicione os serviços do BlackDigital.AspNet
builder.Services.AddBlackDigitalServices();

var app = builder.Build();

// Configure o middleware REST
app.UseBlackDigitalRest();

// Configure o sistema de transformação (opcional)
app.UseBlackDigitalTransforms();

app.Run();
```

### 2. Crie seu primeiro serviço

```csharp
[Service("api/users")]
public class UserService
{
    [Action("GET")]
    public async Task<IEnumerable<User>> GetUsers()
    {
        // Sua lógica aqui
        return await GetAllUsersAsync();
    }

    [Action("POST")]
    public async Task<User> CreateUser([FromBody] CreateUserRequest request)
    {
        // Sua lógica aqui
        return await CreateUserAsync(request);
    }
}
```

## 🎯 Principais Recursos

### 🔧 Sistema REST
Crie APIs REST completas sem controllers tradicionais:
- **Atributos Simples**: `[Service]` e `[Action]` definem rotas automaticamente
- **Binding Automático**: Parâmetros vinculados automaticamente
- **Suporte Completo**: GET, POST, PUT, DELETE, PATCH
- **Async/Await**: Suporte nativo para operações assíncronas

### 🔄 Sistema de Transformação
Evolua suas APIs mantendo compatibilidade:
- **Versionamento Automático**: Transformações transparentes entre versões
- **Compatibilidade Total**: Clientes antigos continuam funcionando
- **Regras Flexíveis**: Defina transformações customizadas
- **Performance**: Zero overhead para versões atuais

### 🔗 Integração Perfeita
Os sistemas trabalham juntos:
- **REST + Transformação**: APIs que evoluem sem quebrar
- **Configuração Unificada**: Setup simples e consistente
- **Monitoramento**: Logs e métricas integradas

## 📚 Documentação Detalhada

### 📖 Guias Completos
- **[Sistema REST](doc/rest-system.pt.md)** - Guia completo do sistema REST
- **[Sistema de Transformação](doc/transform-system.pt.md)** - Guia completo do sistema de transformação

### 🎯 Navegação Rápida
- [Como criar serviços REST](doc/rest-system.pt.md#como-usar)
- [Configuração de transformações](doc/transform-system.pt.md#configuração-e-uso)
- [Exemplos práticos](doc/rest-system.pt.md#exemplos-práticos)
- [Melhores práticas](doc/rest-system.pt.md#melhores-práticas)

## 🚀 Exemplos Rápidos

### Serviço REST Simples
```csharp
[Service("api/products")]
public class ProductService
{
    [Action("GET")]
    public async Task<IEnumerable<Product>> GetProducts()
        => await _repository.GetAllAsync();

    [Action("GET", "{id}")]
    public async Task<Product> GetProduct(int id)
        => await _repository.GetByIdAsync(id);

    [Action("POST")]
    public async Task<Product> CreateProduct([FromBody] Product product)
        => await _repository.CreateAsync(product);
}
```

### Transformação Básica
```csharp
public class UserTransformRule : ITransformRule<OldUser, NewUser>
{
    public NewUser Transform(OldUser source)
    {
        return new NewUser
        {
            Id = source.Id,
            FullName = $"{source.FirstName} {source.LastName}",
            Email = source.Email,
            CreatedAt = source.Created
        };
    }
}
```

### 📁 Projeto de Exemplo Completo
Explore o [projeto de exemplo](example/) que demonstra:
- Configuração completa
- Serviços REST
- Transformações
- Integração com Entity Framework
- Testes unitários

## 📁 Estrutura do Projeto

```
BlackDigital.AspNet/
├── 📁 src/                    # Código fonte principal
│   ├── 📁 Rest/              # Sistema REST
│   ├── 📁 Binder/            # Model binders customizados
│   ├── 📁 Constraint/        # Route constraints
│   ├── 📁 Services/          # Serviços auxiliares
│   └── 📁 Infrastructures/   # Infraestrutura
├── 📁 example/               # Projeto de exemplo
├── 📁 test/                  # Testes unitários
├── 📁 doc/                   # Documentação
│   ├── 📄 rest-system.md     # Documentação do sistema REST
│   ├── 📄 transform-system.md # Documentação de transformações
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