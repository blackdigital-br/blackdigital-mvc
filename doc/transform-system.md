# Sistema de Transformação - BlackDigital.AspNet

## Introdução: Evolução de API com Compatibilidade Garantida

O **Sistema de Transformação** do BlackDigital.AspNet foi criado para resolver um dos maiores desafios no desenvolvimento de APIs: **como evoluir uma API sem quebrar aplicações existentes**.

### O Problema da Evolução de APIs

Quando uma API evolui, surgem dilemas:
- **Quebrar compatibilidade**: Força todos os clientes a atualizarem código simultaneamente
- **Manter versões antigas**: Gera duplicação de código e complexidade de manutenção
- **Versionar endpoints**: Multiplica rotas e controladores

### A Solução: Transformação Automática

O sistema de transformação permite que sua API **evolua naturalmente** enquanto **mantém compatibilidade automática** com versões anteriores:

```csharp
// API v1 (formato antigo) - continua funcionando
POST /api/users
{
  "firstName": "João",
  "lastName": "Silva",
  "email": "joao@email.com"
}

// API v2 (formato novo) - evolução natural
POST /api/users  
{
  "fullName": "João Silva",
  "email": "joao@email.com",
  "preferences": { "language": "pt-BR" }
}

// O sistema automaticamente transforma v1 → v2 na entrada
// E transforma v2 → v1 na saída quando necessário
```

### Benefícios Principais

✅ **Não quebra aplicações existentes** - Clientes antigos continuam funcionando  
✅ **Evolução gradual** - Novos recursos sem impacto em código legado  
✅ **Compatibilidade automática** - Transformações transparentes entre versões  
✅ **Redução de manutenção** - Uma única implementação para múltiplas versões  
✅ **Migração suave** - Clientes podem migrar no seu próprio ritmo  

### Como Funciona

1. **Cliente faz chamada** com formato antigo (v1)
2. **Sistema identifica** a versão baseada em headers/parâmetros
3. **Transformação de entrada** converte v1 → v2 automaticamente
4. **Lógica de negócio** processa dados no formato atual (v2)
5. **Transformação de saída** converte v2 → v1 para o cliente
6. **Cliente recebe** resposta no formato esperado (v1)

### Características Principais

- **Versionamento Inteligente**: Aplica transformações sequenciais entre versões
- **Direcionamento Flexível**: Transformações na entrada (Input) e/ou saída (Output)
- **Encadeamento Automático**: Múltiplas transformações aplicadas sequencialmente
- **Integração com DI**: Suporte completo ao sistema de injeção de dependência
- **Performance Otimizada**: Suporte a operações síncronas e assíncronas

## Exemplo Prático: Evolução de API de Usuários

### Cenário Real: API de Usuários Evoluindo de v1 para v2

**Situação**: Sua API de usuários precisa evoluir, mas você tem aplicações mobile e web já em produção que não podem parar de funcionar.

#### API v1 (Formato Original)
```csharp
// Modelo v1 - formato separado
public class UserV1
{
    public int Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public bool IsActive { get; set; }
}

// Endpoint v1
POST /api/users
{
  "firstName": "João",
  "lastName": "Silva", 
  "email": "joao@email.com",
  "isActive": true
}
```

#### API v2 (Evolução Natural)
```csharp
// Modelo v2 - formato otimizado
public class UserV2
{
    public int Id { get; set; }
    public string FullName { get; set; }  // Combinado
    public string Email { get; set; }
    public string Status { get; set; }    // Enum string em vez de bool
    public UserPreferences Preferences { get; set; } // Novo campo
}

// Endpoint v2 (mesma rota!)
POST /api/users
{
  "fullName": "João Silva",
  "email": "joao@email.com", 
  "status": "Active",
  "preferences": {
    "language": "pt-BR",
    "theme": "dark"
  }
}
```

### Implementação das Transformações

#### 1. Transformação de Entrada (v1 → v2)
```csharp
public class UserV1ToV2InputRule : TransformRule<UserV1, UserV2>
{
    public override UserV2? Transform(UserV1? userV1)
    {
        if (userV1 == null) return null;

        return new UserV2
        {
            Id = userV1.Id,
            FullName = CombineNames(userV1.FirstName, userV1.LastName),
            Email = userV1.Email,
            Status = userV1.IsActive ? "Active" : "Inactive",
            Preferences = new UserPreferences 
            { 
                Language = "pt-BR", // Padrão para dados v1
                Theme = "light"     // Padrão para dados v1
            }
        };
    }

    private string CombineNames(string firstName, string lastName)
    {
        return $"{firstName?.Trim()} {lastName?.Trim()}".Trim();
    }
}
```

#### 2. Transformação de Saída (v2 → v1)
```csharp
public class UserV2ToV1OutputRule : TransformRule<UserV2, UserV1>
{
    public override UserV1? Transform(UserV2? userV2)
    {
        if (userV2 == null) return null;

        var (firstName, lastName) = SplitFullName(userV2.FullName);

        return new UserV1
        {
            Id = userV2.Id,
            FirstName = firstName,
            LastName = lastName,
            Email = userV2.Email,
            IsActive = userV2.Status == "Active"
        };
    }

    private (string firstName, string lastName) SplitFullName(string fullName)
    {
        if (string.IsNullOrEmpty(fullName)) return ("", "");
        
        var parts = fullName.Split(' ', 2);
        return (parts[0], parts.Length > 1 ? parts[1] : "");
    }
}
```

#### 3. Registro das Transformações
```csharp
// Startup.cs ou Program.cs
builder.Services.AddTransform(config =>
{
    // Transformação de entrada: v1 → v2
    config.AddInputRule<UserV1ToV2InputRule>("POST:api/users/", "2024-02-01");
    
    // Transformação de saída: v2 → v1  
    config.AddOutputRule<UserV2ToV1OutputRule>("GET:api/users/", "2024-02-01");
    config.AddOutputRule<UserV2ToV1OutputRule>("POST:api/users/", "2024-02-01");
});
```

### Resultado: Compatibilidade Total

#### Cliente v1 (aplicação mobile antiga)
```csharp
// Cliente envia formato v1
var request = new UserV1 
{
    FirstName = "Maria",
    LastName = "Santos",
    Email = "maria@email.com",
    IsActive = true
};

// Sistema automaticamente:
// 1. Recebe UserV1
// 2. Transforma para UserV2 (entrada)
// 3. Processa como UserV2 internamente
// 4. Transforma resposta para UserV1 (saída)
// 5. Cliente recebe UserV1

// Cliente recebe formato v1 (como esperado)
var response = new UserV1
{
    Id = 123,
    FirstName = "Maria", 
    LastName = "Santos",
    Email = "maria@email.com",
    IsActive = true
};
```

#### Cliente v2 (aplicação web nova)
```csharp
// Cliente envia formato v2 diretamente
var request = new UserV2
{
    FullName = "Maria Santos",
    Email = "maria@email.com",
    Status = "Active",
    Preferences = new UserPreferences { Language = "pt-BR" }
};

// Sistema processa diretamente (sem transformação)
// Cliente recebe formato v2 com todos os novos campos
```

### Vantagens Desta Abordagem

✅ **Zero downtime**: Aplicações antigas continuam funcionando  
✅ **Migração gradual**: Cada cliente migra quando conveniente  
✅ **Código único**: Uma implementação serve múltiplas versões  
✅ **Evolução contínua**: Novos recursos sem impacto em legado  
✅ **Manutenção simplificada**: Transformações centralizadas  

## Como Criar Regras de Transformação - Guia Prático

### Processo Passo a Passo para Criar Regras

#### 1. Identificar a Necessidade de Transformação

**Perguntas essenciais:**
- **O que precisa ser transformado?** (tipo de dados, propriedades específicas)
- **Por que transformar?** (versionamento, compatibilidade, validação)
- **Quando aplicar?** (versão específica da API, endpoint específico)
- **Direção da transformação:** Entrada (Input) ou Saída (Output)?

**Exemplo prático de análise:**
```
Cenário: API evoluiu de v1 para v2
- v1: { "firstName": "João", "lastName": "Silva" }
- v2: { "fullName": "João Silva" }

Necessidade: 
- Input: Converter v1 → v2 quando cliente envia formato antigo
- Output: Converter v2 → v1 quando cliente espera formato antigo
```

#### 2. Escolher o Tipo de Regra Adequado

**Use `TransformRule<T>` para modificações no mesmo tipo:**

```csharp
// Cenário: Sanitizar dados de entrada
public class UserSanitizationRule : TransformRule<SaveUser>
{
    public override SaveUser? Transform(SaveUser? user)
    {
        if (user == null) return null;
        
        // Limpar e validar dados
        user.Email = user.Email?.Trim().ToLowerInvariant();
        user.Name = user.Name?.Trim();
        
        return user;
    }
}
```

**Use `TransformRule<TIn, TOut>` para conversões entre tipos:**

```csharp
// Cenário: Migração de versão
public class UserV1ToV2Rule : TransformRule<UserV1, UserV2>
{
    public override UserV2? Transform(UserV1? oldUser)
    {
        if (oldUser == null) return null;
        
        return new UserV2
        {
            Id = oldUser.Id,
            FullName = $"{oldUser.FirstName} {oldUser.LastName}".Trim(),
            Email = oldUser.Email
        };
    }
}
```

#### 3. Quando Criar Nova Regra vs Modificar Existente

**Crie uma NOVA regra quando:**
- Mudança de versão da API
- Novo endpoint com necessidades específicas
- Lógica de transformação completamente diferente
- Diferentes clientes precisam de transformações distintas

**Modifique regra EXISTENTE quando:**
- Correção de bug na transformação atual
- Melhoria na mesma lógica de transformação
- Adição de validação à transformação existente

#### 4. Padrões Comuns de Transformação

**Padrão 1: Combinação de Campos**
```csharp
public class CombineFieldsRule : TransformRule<UserInput, User>
{
    public override User? Transform(UserInput? input)
    {
        if (input == null) return null;
        
        return new User
        {
            FullName = $"{input.FirstName} {input.LastName}".Trim(),
            DisplayName = string.IsNullOrEmpty(input.Nickname) 
                ? input.FirstName 
                : input.Nickname
        };
    }
}
```

**Padrão 2: Separação de Campos**
```csharp
public class SplitFieldsRule : TransformRule<User, UserOutput>
{
    public override UserOutput? Transform(User? user)
    {
        if (user == null) return null;
        
        var nameParts = user.FullName?.Split(' ', 2) ?? new[] { "", "" };
        
        return new UserOutput
        {
            FirstName = nameParts[0],
            LastName = nameParts.Length > 1 ? nameParts[1] : ""
        };
    }
}
```

**Padrão 3: Mapeamento de Enums/Códigos**
```csharp
public class StatusMappingRule : TransformRule<LegacyUser, ModernUser>
{
    public override ModernUser? Transform(LegacyUser? legacy)
    {
        if (legacy == null) return null;
        
        return new ModernUser
        {
            Status = MapLegacyStatus(legacy.StatusCode),
            Priority = MapPriorityLevel(legacy.Level)
        };
    }
    
    private UserStatus MapLegacyStatus(int code) => code switch
    {
        1 => UserStatus.Active,
        2 => UserStatus.Inactive,
        3 => UserStatus.Suspended,
        _ => UserStatus.Unknown
    };
}
```

**Padrão 4: Validação e Sanitização**
```csharp
public class ValidationRule : TransformRule<UserInput>
{
    public override UserInput? Transform(UserInput? input)
    {
        if (input == null) return null;
        
        // Sanitização
        input.Email = input.Email?.Trim().ToLowerInvariant();
        input.Phone = CleanPhoneNumber(input.Phone);
        
        // Validação
        if (!IsValidEmail(input.Email))
            throw new ArgumentException("Email inválido");
            
        return input;
    }
    
    private string? CleanPhoneNumber(string? phone)
    {
        return phone?.Replace("(", "").Replace(")", "").Replace("-", "").Replace(" ", "");
    }
}
```

#### 3. Implementar a Regra

**Estrutura básica:**

```csharp
public class MinhaTransformRule : TransformRule<TipoEntrada, TipoSaida>
{
    // Construtor para dependências (opcional)
    public MinhaTransformRule(IDependencia dependencia)
    {
        _dependencia = dependencia;
    }

    public override TipoSaida? Transform(TipoEntrada? value)
    {
        // Verificar se valor é nulo
        if (value == null)
            return null; // ou valor padrão

        // Implementar lógica de transformação
        return new TipoSaida
        {
            // Mapear propriedades
        };
    }

    // Para operações assíncronas (opcional)
    public override async Task<TipoSaida?> TransformAsync(TipoEntrada? value)
    {
        // Lógica assíncrona
        return await ProcessAsync(value);
    }
}
```

#### 4. Registrar a Regra

```csharp
builder.Services.AddTransform(transformConfig =>
    transformConfig
        .AddInputRule<MinhaTransformRule>("POST:api/endpoint/", "2024-01-01")
        .AddOutputRule<OutraTransformRule>("POST:api/endpoint/", "2024-01-01")
);
```

## Cenários de Uso Comuns - Exemplos Práticos

### 1. Migração de Versões de API

**Cenário Real**: Sua API evoluiu e você precisa manter compatibilidade com clientes antigos.

**Problema**: 
- API v1: `{ "firstName": "João", "lastName": "Silva", "created": "2024-01-01" }`
- API v2: `{ "fullName": "João Silva", "createdAt": "2024-01-01T10:00:00Z", "isActive": true }`

**Solução Completa**:

```csharp
// Transformação de entrada: v1 → v2
public class UserV1ToV2InputRule : TransformRule<UserV1, UserV2>
{
    public override UserV2? Transform(UserV1? oldUser)
    {
        if (oldUser == null) return null;

        return new UserV2
        {
            Id = oldUser.Id,
            FullName = CombineNames(oldUser.FirstName, oldUser.LastName),
            Email = oldUser.Email,
            CreatedAt = oldUser.Created ?? DateTime.UtcNow,
            IsActive = true // Novo campo com valor padrão
        };
    }
    
    private string CombineNames(string? firstName, string? lastName)
    {
        return $"{firstName?.Trim()} {lastName?.Trim()}".Trim();
    }
}

// Transformação de saída: v2 → v1
public class UserV2ToV1OutputRule : TransformRule<UserV2, UserV1>
{
    public override UserV1? Transform(UserV2? newUser)
    {
        if (newUser == null) return null;

        var (firstName, lastName) = SplitFullName(newUser.FullName);
        
        return new UserV1
        {
            Id = newUser.Id,
            FirstName = firstName,
            LastName = lastName,
            Email = newUser.Email,
            Created = newUser.CreatedAt
        };
    }
    
    private (string firstName, string lastName) SplitFullName(string? fullName)
    {
        if (string.IsNullOrEmpty(fullName))
            return ("", "");
            
        var parts = fullName.Split(' ', 2);
        return (parts[0], parts.Length > 1 ? parts[1] : "");
    }
}

// Registro das regras
builder.Services.AddTransform(config =>
    config
        .AddInputRule<UserV1ToV2InputRule>("POST:api/users/", "2024-02-01")
        .AddOutputRule<UserV2ToV1OutputRule>("GET:api/users/", "2024-02-01")
);
```

### 2. Transformação de Dados Legados

**Cenário Real**: Integração com sistema legado que usa formato e convenções diferentes.

**Problema**: 
- Sistema legado: `{ "user_id": 123, "user_name": "JOÃO", "email_addr": "JOAO@EMAIL.COM", "status_cd": 1, "dept_code": "IT" }`
- Sistema moderno: `{ "id": 123, "name": "João", "email": "joao@email.com", "status": "Active", "department": { "code": "IT", "name": "Tecnologia da Informação" } }`

**Solução Completa**:

```csharp
public class LegacyToModernUserRule : TransformRule<LegacyUser, ModernUser>
{
    private readonly IDepartmentService _departmentService;
    
    public LegacyToModernUserRule(IDepartmentService departmentService)
    {
        _departmentService = departmentService;
    }

    public override ModernUser? Transform(LegacyUser? legacy)
    {
        if (legacy == null) return null;

        return new ModernUser
        {
            Id = legacy.UserId,
            Name = NormalizeName(legacy.UserName),
            Email = NormalizeEmail(legacy.EmailAddress),
            Status = MapStatusCode(legacy.StatusCode),
            Department = MapDepartment(legacy.DeptCode),
            CreatedAt = legacy.CreateDate ?? DateTime.UtcNow,
            LastModified = DateTime.UtcNow
        };
    }
    
    private string? NormalizeName(string? name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        
        // Converter de MAIÚSCULO para Título
        return System.Globalization.CultureInfo.CurrentCulture.TextInfo
            .ToTitleCase(name.ToLowerInvariant());
    }
    
    private string? NormalizeEmail(string? email)
    {
        return email?.Trim().ToLowerInvariant();
    }
    
    private UserStatus MapStatusCode(int statusCode) => statusCode switch
    {
        1 => UserStatus.Active,
        2 => UserStatus.Inactive,
        3 => UserStatus.Suspended,
        9 => UserStatus.Deleted,
        _ => UserStatus.Unknown
    };
    
    private Department? MapDepartment(string? deptCode)
    {
        if (string.IsNullOrEmpty(deptCode)) return null;
        
        return _departmentService.GetByCode(deptCode);
    }
}

// Para transformação assíncrona com consulta ao banco
public class LegacyToModernUserAsyncRule : TransformRule<LegacyUser, ModernUser>
{
    private readonly IDepartmentRepository _departmentRepo;
    
    public LegacyToModernUserAsyncRule(IDepartmentRepository departmentRepo)
    {
        _departmentRepo = departmentRepo;
    }

    public override async Task<ModernUser?> TransformAsync(LegacyUser? legacy)
    {
        if (legacy == null) return null;

        var department = await _departmentRepo.GetByCodeAsync(legacy.DeptCode);
        
        return new ModernUser
        {
            Id = legacy.UserId,
            Name = NormalizeName(legacy.UserName),
            Email = legacy.EmailAddress?.ToLowerInvariant(),
            Status = MapStatusCode(legacy.StatusCode),
            Department = department
        };
    }
}
```

### 3. Validação e Sanitização de Dados

**Cenário Real**: Dados vindos de formulários web precisam ser limpos e validados.

**Problema**: 
- Entrada: `{ "email": "  JOAO@EMAIL.COM  ", "phone": "(11) 99999-9999", "name": "  joão silva  " }`
- Saída esperada: `{ "email": "joao@email.com", "phone": "11999999999", "name": "João Silva" }`

**Solução**:

```csharp
public class UserInputSanitizationRule : TransformRule<UserInput>
{
    public override UserInput? Transform(UserInput? input)
    {
        if (input == null) return null;

        // Sanitização
        input.Email = SanitizeEmail(input.Email);
        input.Phone = SanitizePhone(input.Phone);
        input.Name = SanitizeName(input.Name);
        input.Document = SanitizeDocument(input.Document);
        
        // Validação
        ValidateInput(input);
        
        return input;
    }
    
    private string? SanitizeEmail(string? email)
    {
        return email?.Trim().ToLowerInvariant();
    }
    
    private string? SanitizePhone(string? phone)
    {
        if (string.IsNullOrEmpty(phone)) return phone;
        
        // Remove todos os caracteres não numéricos
        return new string(phone.Where(char.IsDigit).ToArray());
    }
    
    private string? SanitizeName(string? name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        
        // Remove espaços extras e converte para título
        var cleaned = string.Join(" ", name.Split(' ', StringSplitOptions.RemoveEmptyEntries));
        return System.Globalization.CultureInfo.CurrentCulture.TextInfo
            .ToTitleCase(cleaned.ToLowerInvariant());
    }
    
    private string? SanitizeDocument(string? document)
    {
        if (string.IsNullOrEmpty(document)) return document;
        
        // Remove pontos, hífens e espaços
        return document.Replace(".", "").Replace("-", "").Replace(" ", "");
    }
    
    private void ValidateInput(UserInput input)
    {
        var errors = new List<string>();
        
        if (string.IsNullOrEmpty(input.Email))
            errors.Add("Email é obrigatório");
        else if (!IsValidEmail(input.Email))
            errors.Add("Email inválido");
            
        if (string.IsNullOrEmpty(input.Name))
            errors.Add("Nome é obrigatório");
        else if (input.Name.Length < 2)
            errors.Add("Nome deve ter pelo menos 2 caracteres");
            
        if (!string.IsNullOrEmpty(input.Phone) && !IsValidPhone(input.Phone))
            errors.Add("Telefone inválido");
            
        if (errors.Any())
            throw new ValidationException(string.Join("; ", errors));
    }
    
    private bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
    
    private bool IsValidPhone(string phone)
    {
        // Telefone brasileiro: 10 ou 11 dígitos
        return phone.Length >= 10 && phone.Length <= 11 && phone.All(char.IsDigit);
    }
}
```

### 4. Adaptação de Formatos para Diferentes Clientes

**Cenário Real**: Diferentes clientes (web, mobile, API externa) precisam de formatos específicos.

**Problema**: 
- Dados internos: `User` completo com todas as propriedades
- Cliente mobile: Formato compacto para economizar dados
- Cliente web: Formato expandido com informações extras

**Solução**:

```csharp
// Para cliente mobile - formato compacto
public class UserToMobileFormatRule : TransformRule<User, MobileUserResponse>
{
    public override MobileUserResponse? Transform(User? user)
    {
        if (user == null) return null;

        return new MobileUserResponse
        {
            Id = user.Id,
            Name = user.Name,
            Avatar = user.ProfilePicture?.ThumbnailUrl, // Apenas thumbnail
            Status = user.IsActive ? "A" : "I", // Formato compacto
            LastSeen = user.LastLoginAt?.ToString("yyyy-MM-dd") // Data simples
        };
    }
}

// Para cliente web - formato expandido
public class UserToWebFormatRule : TransformRule<User, WebUserResponse>
{
    public override WebUserResponse? Transform(User? user)
    {
        if (user == null) return null;

        return new WebUserResponse
        {
            Id = user.Id,
            FullName = user.Name,
            Email = user.Email,
            ProfilePicture = new ProfilePictureInfo
            {
                OriginalUrl = user.ProfilePicture?.OriginalUrl,
                ThumbnailUrl = user.ProfilePicture?.ThumbnailUrl,
                MediumUrl = user.ProfilePicture?.MediumUrl
            },
            Status = new UserStatusInfo
            {
                IsActive = user.IsActive,
                StatusText = user.IsActive ? "Ativo" : "Inativo",
                LastLoginAt = user.LastLoginAt,
                LastLoginFormatted = user.LastLoginAt?.ToString("dd/MM/yyyy HH:mm")
            },
            Permissions = user.Roles?.Select(r => r.Name).ToList() ?? new List<string>(),
            Metadata = new Dictionary<string, object>
            {
                ["createdAt"] = user.CreatedAt,
                ["totalLogins"] = user.LoginCount,
                ["accountType"] = user.AccountType.ToString()
            }
        };
    }
}
```

#### 3. Validação e Sanitização

**Problema**: Dados de entrada precisam ser validados/limpos.

**Solução**:
```csharp
public class UserSanitizationRule : TransformRule<SaveUser>
{
    public override SaveUser? Transform(SaveUser? user)
    {
        if (user == null) return null;

        // Sanitizar dados
        user.Email = user.Email?.Trim().ToLowerInvariant();
        user.Name = user.Name?.Trim();
        
        // Validar
        if (string.IsNullOrEmpty(user.Email))
            throw new ArgumentException("Email é obrigatório");

        if (!IsValidEmail(user.Email))
            throw new ArgumentException("Email inválido");

        return user;
    }

    private bool IsValidEmail(string email)
    {
        // Implementar validação de email
        return email.Contains("@") && email.Contains(".");
    }
}
```

#### 4. Adaptação de Formatos

**Problema**: Cliente espera formato específico.

**Solução**:
```csharp
public class UserToApiResponseRule : TransformRule<User, ApiUserResponse>
{
    public override ApiUserResponse? Transform(User? user)
    {
        if (user == null) return null;

        return new ApiUserResponse
        {
            Id = user.Id.ToString(), // Converter para string
            DisplayName = user.Name,
            ContactInfo = new ContactInfo
            {
                Email = user.Email,
                Phone = user.PhoneNumber
            },
            Metadata = new Dictionary<string, object>
            {
                ["created"] = user.CreatedAt.ToString("yyyy-MM-dd"),
                ["lastLogin"] = user.LastLoginAt?.ToString("yyyy-MM-dd HH:mm:ss")
            }
        };
    }
}
```

## Guia Prático de Implementação

### Workflow Completo

#### 1. Análise da Necessidade

**Perguntas a fazer:**
- Qual versão da API está sendo alterada?
- O que mudou entre as versões?
- Quais clientes serão afetados?
- É uma mudança breaking ou compatível?

**Exemplo prático:**
```
Cenário: API v1 tinha campos separados (firstName, lastName)
         API v2 tem campo único (fullName)
         
Decisão: Criar transformação para converter v1 → v2 na entrada
         Criar transformação para converter v2 → v1 na saída
```

#### 2. Design da Transformação

**Mapeamento de campos:**
```csharp
// Documentar o mapeamento
/*
UserV1 → UserV2:
- firstName + lastName → fullName
- created → createdAt
- (novo) isActive = true (padrão)

UserV2 → UserV1:
- fullName → firstName (primeira palavra), lastName (resto)
- createdAt → created
- isActive → (ignorar, não existe em v1)
*/
```

#### 3. Implementação

**Transformação de entrada (v1 → v2):**
```csharp
public class UserV1ToV2InputRule : TransformRule<UserV1, UserV2>
{
    public override UserV2? Transform(UserV1? v1)
    {
        if (v1 == null) return null;

        return new UserV2
        {
            Id = v1.Id,
            FullName = $"{v1.FirstName} {v1.LastName}".Trim(),
            Email = v1.Email,
            CreatedAt = v1.Created,
            IsActive = true
        };
    }
}
```

**Transformação de saída (v2 → v1):**
```csharp
public class UserV2ToV1OutputRule : TransformRule<UserV2, UserV1>
{
    public override UserV1? Transform(UserV2? v2)
    {
        if (v2 == null) return null;

        var nameParts = v2.FullName?.Split(' ', 2) ?? new[] { "", "" };
        
        return new UserV1
        {
            Id = v2.Id,
            FirstName = nameParts[0],
            LastName = nameParts.Length > 1 ? nameParts[1] : "",
            Email = v2.Email,
            Created = v2.CreatedAt
        };
    }
}
```

#### 4. Registro e Configuração

```csharp
builder.Services.AddTransform(transformConfig =>
    transformConfig
        // Entrada: v1 → v2
        .AddInputRule<UserV1ToV2InputRule>("POST:api/users/", "2024-02-01")
        .AddInputRule<UserV1ToV2InputRule>("PUT:api/users/{id}", "2024-02-01")
        
        // Saída: v2 → v1
        .AddOutputRule<UserV2ToV1OutputRule>("GET:api/users/", "2024-02-01")
        .AddOutputRule<UserV2ToV1OutputRule>("GET:api/users/{id}", "2024-02-01")
        .AddOutputRule<UserV2ToV1OutputRule>("POST:api/users/", "2024-02-01")
        .AddOutputRule<UserV2ToV1OutputRule>("PUT:api/users/{id}", "2024-02-01")
);
```

#### 5. Testes

**Teste unitário da transformação:**
```csharp
[Test]
public void UserV1ToV2_ShouldCombineNames()
{
    // Arrange
    var rule = new UserV1ToV2InputRule();
    var v1User = new UserV1
    {
        Id = 1,
        FirstName = "João",
        LastName = "Silva",
        Email = "joao@email.com",
        Created = DateTime.UtcNow
    };

    // Act
    var result = rule.Transform(v1User);

    // Assert
    Assert.That(result.FullName, Is.EqualTo("João Silva"));
    Assert.That(result.IsActive, Is.True);
}
```

### Quando Criar Nova Regra vs. Modificar Existente

#### Criar Nova Regra Quando:
- **Nova versão da API**: Sempre criar regra específica para nova versão
- **Novo endpoint**: Endpoint diferente pode ter necessidades diferentes
- **Lógica completamente diferente**: Transformação tem propósito diferente

#### Modificar Regra Existente Quando:
- **Bug fix**: Correção de comportamento incorreto
- **Melhoria de performance**: Otimização sem mudança de comportamento
- **Refatoração**: Melhorar código sem alterar resultado

##### Exemplo de Evolução:

```csharp
// Versão 1.0 - Regra inicial
public class UserTransformV1 : TransformRule<User>
{
    public override User? Transform(User? user)
    {
        if (user != null)
            user.Email = user.Email?.ToLowerInvariant();
        return user;
    }
}

// Versão 2.0 - Nova regra (não modificar a v1)
public class UserTransformV2 : TransformRule<User>
{
    public override User? Transform(User? user)
    {
        if (user != null)
        {
            user.Email = user.Email?.ToLowerInvariant();
            user.Name = user.Name?.Trim(); // Nova funcionalidade
        }
        return user;
    }
}
```

### Padrões Comuns de Transformação

#### 1. Padrão de Mapeamento Simples
```csharp
public class SimpleMapRule : TransformRule<Source, Target>
{
    public override Target? Transform(Source? source)
    {
        return source == null ? null : new Target
        {
            Property1 = source.Property1,
            Property2 = source.Property2
        };
    }
}
```

#### 2. Padrão de Enriquecimento
```csharp
public class EnrichmentRule : TransformRule<User>
{
    private readonly IUserService _userService;

    public EnrichmentRule(IUserService userService)
    {
        _userService = userService;
    }

    public override async Task<User?> TransformAsync(User? user)
    {
        if (user != null)
        {
            user.LastLoginAt = await _userService.GetLastLoginAsync(user.Id);
        }
        return user;
    }
}
```

#### 3. Padrão de Validação
```csharp
public class ValidationRule : TransformRule<User>
{
    public override User? Transform(User? user)
    {
        if (user == null) return null;

        if (string.IsNullOrEmpty(user.Email))
            throw new ValidationException("Email é obrigatório");

        if (user.Age < 0 || user.Age > 150)
            throw new ValidationException("Idade inválida");

        return user;
    }
}
```

#### 4. Padrão de Normalização
```csharp
public class NormalizationRule : TransformRule<User>
{
    public override User? Transform(User? user)
    {
        if (user == null) return null;

        user.Email = user.Email?.Trim().ToLowerInvariant();
        user.Name = NormalizeName(user.Name);
        user.PhoneNumber = NormalizePhone(user.PhoneNumber);

        return user;
    }

    private string? NormalizeName(string? name)
    {
        return name?.Trim()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(word => char.ToUpper(word[0]) + word[1..].ToLower())
            .Aggregate((a, b) => $"{a} {b}");
    }

    private string? NormalizePhone(string? phone)
    {
        return phone?.Replace("(", "").Replace(")", "")
            .Replace("-", "").Replace(" ", "");
    }
}
```

## Detalhamento Técnico: Arquitetura e Classes

### Arquitetura do Sistema

### Componentes Principais

1. **ITransformRule**: Interface base para todas as regras de transformação
2. **TransformRule**: Classes base abstratas para implementação de regras
3. **TransformKey**: Identificador único para regras de transformação
4. **TransformDirection**: Enum que define a direção da transformação
5. **TransformBuilder**: Builder pattern para configuração de regras
6. **TransformManager**: Gerenciador principal que executa as transformações
7. **TransformExtensions**: Extensões para integração com DI

### Documentação das Classes

#### ITransformRule

Interface base que define o contrato para todas as regras de transformação.

```csharp
public interface ITransformRule
{
    Type? InputType { get; }
    Type? OutputType { get; }
    object? Transform(object? value);
    Task<object?> TransformAsync(object? value);
}
```

##### Variações Genéricas

- **ITransformRule&lt;T&gt;**: Para transformações do mesmo tipo
- **ITransformRule&lt;TIn, TOut&gt;**: Para transformações entre tipos diferentes

#### TransformRule

Classes base abstratas que implementam ITransformRule com comportamentos padrão.

#### TransformRule (Base)
```csharp
public abstract class TransformRule : ITransformRule
{
    public virtual Type? InputType => null;
    public virtual Type? OutputType => null;
    public virtual object? Transform(object? value) => value;
    public virtual Task<object?> TransformAsync(object? value) => Task.FromResult(Transform(value));
}
```

#### TransformRule&lt;T&gt;
Para transformações que mantêm o mesmo tipo:
```csharp
public abstract class TransformRule<T> : ITransformRule<T>
{
    public virtual Type? InputType => typeof(T);
    public virtual Type? OutputType => typeof(T);
    public virtual T? Transform(T? value) => value;
    public virtual Task<T?> TransformAsync(T? value) => Task.FromResult(Transform(value));
}
```

#### TransformRule&lt;TIn, TOut&gt;
Para transformações entre tipos diferentes:
```csharp
public abstract class TransformRule<TIn, TOut> : ITransformRule<TIn, TOut>
{
    public virtual Type? InputType => typeof(TIn);
    public virtual Type? OutputType => typeof(TOut);
    public virtual TOut? Transform(TIn? value) => default;
    public virtual Task<TOut?> TransformAsync(TIn? value) => Task.FromResult(Transform(value));
}
```

#### TransformDirection

Enum que define a direção da transformação:

```csharp
[Flags]
public enum TransformDirection
{
    Input = 1,      // Transformação de entrada
    Output = 2,     // Transformação de saída
    Both = Input | Output  // Ambas as direções
}
```

#### TransformDirectionHelper

Classe helper com métodos de extensão para trabalhar com TransformDirection:

```csharp
public static class TransformDirectionHelper
{
    public static bool HasInput(this TransformDirection direction);
    public static bool HasOutput(this TransformDirection direction);
    public static IEnumerable<TransformDirection> Enumerate(this TransformDirection direction);
}
```

#### TransformKey

Struct que identifica unicamente uma regra de transformação:

```csharp
public readonly struct TransformKey : IEquatable<TransformKey>
{
    public TransformDirection Direction { get; }
    public string Key { get; }
    public string Version { get; }
    
    public TransformKey(string key, string version, TransformDirection direction);
    public TransformKey(string key, string version); // Padrão: Input
}
```

##### Exemplo de Uso
```csharp
var key = new TransformKey("POST:api/user/", "2025-10-08", TransformDirection.Input);
```

#### TransformBuilder

Implementa o padrão Builder para configuração de regras de transformação:

##### Métodos Principais

```csharp
// Adicionar regra com instância
public TransformBuilder AddRule(TransformKey key, ITransformRule rule);
public TransformBuilder AddRule(string key, string version, ITransformRule rule, TransformDirection direction = TransformDirection.Input);

// Adicionar regra com tipo (para DI)
public TransformBuilder AddRule(TransformKey key, Type ruleType);
public TransformBuilder AddRule<TRule>(string key, string version, TransformDirection direction = TransformDirection.Input) where TRule : ITransformRule;

// Métodos de conveniência
public TransformBuilder AddInputRule(string key, string version, ITransformRule rule);
public TransformBuilder AddOutputRule(string key, string version, ITransformRule rule);
public TransformBuilder AddInputAndOutputRule(string key, string version, ITransformRule rule);
```

#### TransformManager

Gerenciador principal que executa as transformações:

##### Métodos Principais

```csharp
// Verificar se existe regra
public bool HasRule(TransformKey key);
public bool HasRule(string key, string version, TransformDirection direction = TransformDirection.Input);

// Obter regras necessárias
public IList<ITransformRule>? GetRequiredRules(TransformKey key);

// Executar transformações
public object? Transform(TransformKey key, object? value);
public object? Transform(string key, string version, object? value, TransformDirection direction = TransformDirection.Input);
public async Task<object?> TransformAsync(TransformKey key, object? value);
```

#### TransformExtensions

Extensões para integração com o sistema de DI do ASP.NET Core:

```csharp
public static IServiceCollection AddTransform(this IServiceCollection services,
    Func<TransformBuilder, TransformBuilder> configBuilder)
```

## Exemplos Avançados e Configuração

### Exemplos Práticos Avançados

#### 1. Transformação Simples (Mesmo Tipo)

```csharp
public class SaveUserTransformRule : TransformRule<SaveUser>
{
    public SaveUserTransformRule(string name)
    {
        Name = name;
    }

    public string Name { get; set; }

    public override SaveUser? Transform(SaveUser? value)
    {
        if (value != null)
        {
            value.Email += $".{Name}";
        }
        return base.Transform(value);
    }
}
```

### 2. Transformação Entre Tipos Diferentes

```csharp
public class OldSaveUsertoSaveUserTransformRule : TransformRule<OldSaveUser, SaveUser>
{
    public override SaveUser? Transform(OldSaveUser? value)
    {
        if (value == null)
            return null;

        return new SaveUser
        {
            Name = value.N,
            Email = value.E,
            Password = value.P
        };
    }
}
```

### 3. Extração de Propriedade

```csharp
public class SaveUserIdTransformRule : TransformRule<SaveUser, int>
{
    public override int Transform(SaveUser? value)
    {
        return value?.Id ?? 12;
    }
}
```

### Configuração e Uso

#### 1. Configuração no Program.cs

```csharp
builder.Services.AddTransform(transformConfig =>
    transformConfig
        // Regra de entrada para versão específica
        .AddInputRule<OldSaveUsertoSaveUserTransformRule>("POST:api/user/", "2020-10-10")
        
        // Regra de saída para versão específica
        .AddOutputRule<SaveUserIdTransformRule>("POST:api/user/", "2020-10-10")
        
        // Regras com instâncias personalizadas
        .AddRule("POST:api/user/", "2025-10-08", new SaveUserTransformRule("input"))
        .AddRule("POST:api/user/", "2025-10-08", new SaveUserTransformRule("output"), TransformDirection.Output)
);
```

### 2. Uso no Código

```csharp
public class UserController : ControllerBase
{
    private readonly TransformManager _transformManager;

    public UserController(TransformManager transformManager)
    {
        _transformManager = transformManager;
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] object userData)
    {
        // Transformação de entrada
        var transformedInput = await _transformManager.TransformAsync(
            "POST:api/user/", "2020-10-10", userData, TransformDirection.Input);

        // Processar dados...
        var result = ProcessUser(transformedInput);

        // Transformação de saída
        var transformedOutput = await _transformManager.TransformAsync(
            "POST:api/user/", "2020-10-10", result, TransformDirection.Output);

        return Ok(transformedOutput);
    }
}
```

### Versionamento e Evolução

#### Como Funciona o Versionamento

O sistema aplica automaticamente todas as transformações necessárias entre a versão solicitada e a versão atual:

1. **Input**: Aplica transformações da versão mais antiga para a mais nova
2. **Output**: Aplica transformações da versão mais nova para a mais antiga

### Exemplo de Evolução

```csharp
// Versão 1.0 -> 2.0: Adicionar campo
transformConfig.AddInputRule<AddFieldTransformRule>("api/user", "2.0");

// Versão 2.0 -> 3.0: Renomear campo
transformConfig.AddInputRule<RenameFieldTransformRule>("api/user", "3.0");

// Versão 3.0 -> 4.0: Validação adicional
transformConfig.AddInputRule<ValidationTransformRule>("api/user", "4.0");
```

### Boas Práticas

#### 1. Nomenclatura de Chaves
- Use padrões consistentes: `"MÉTODO:rota"`
- Exemplo: `"POST:api/user/"`, `"GET:api/user/{id}"`

#### 2. Versionamento
- Use formato de data: `"YYYY-MM-DD"`
- Mantenha ordem cronológica para funcionamento correto

#### 3. Separação de Responsabilidades
- Uma transformação por responsabilidade
- Prefira múltiplas transformações simples a uma complexa

#### 4. Testes
- Teste cada transformação individualmente
- Teste cenários de encadeamento
- Verifique comportamento com valores nulos

#### 5. Performance
- Use transformações síncronas quando possível
- Considere cache para transformações custosas
- Monitore performance em cenários de alto volume

### Integração com Middleware REST

O sistema de transformação é automaticamente integrado com o RestMiddleware do BlackDigital.AspNet, aplicando transformações baseadas na rota e versão da API solicitada.

Para mais informações sobre o RestMiddleware, consulte a documentação específica do sistema REST.