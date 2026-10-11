# Segurança

🇧🇷 Português | [🇺🇸 English](security.md)

O módulo de Segurança oferece dois blocos prontos para uso: um **serviço de tokens JWT** e um **serviço de hash de senhas Argon2id**.

## Serviço de Token JWT

O `ITokenService` / `JwtService` cuida da geração, validação e inspeção de tokens JWT.

### Configuração

O serviço é configurado através da seção `Jwt` no `appsettings.json`:

```json
{
  "Jwt": {
    "SecretKey": "sua-chave-secreta-bem-longa-com-pelo-menos-32-caracteres",
    "Issuer": "https://seu-app.com",
    "Audience": "https://seu-app.com",
    "AccessTokenExpirationMinutes": 60,
    "RefreshTokenExpirationDays": 30
  }
}
```

Registre-o no `Program.cs`:

```csharp
using BlackDigital.AspNet.Security;

builder.Services.AddJwtService(builder.Configuration);
```

Alternativamente, configure-o inline sem uma seção `Jwt`:

```csharp
builder.Services.AddJwtService(
    secretKey: "sua-chave-secreta-bem-longa-com-pelo-menos-32-caracteres",
    issuer: "https://seu-app.com",
    audience: "https://seu-app.com",
    accessTokenExpirationMinutes: 60,
    refreshTokenExpirationDays: 30
);
```

### Gerando Tokens

Injete `ITokenService` e gere tokens de acesso e de atualização:

```csharp
public class AuthService
{
    private readonly ITokenService _tokenService;

    public AuthService(ITokenService tokenService)
    {
        _tokenService = tokenService;
    }

    public (string accessToken, string refreshToken) Login(User user)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Email),
            new Claim(ClaimTypes.Role, user.Role)
        };

        var accessToken = _tokenService.GenerateToken(claims);
        var refreshToken = _tokenService.GenerateRefreshToken(claims);

        return (accessToken, refreshToken);
    }
}
```

### Validando e Inspecionando Tokens

```csharp
// Valida um token e retorna o ClaimsPrincipal
ClaimsPrincipal? principal = _tokenService.ValidateToken(token);

if (principal != null)
{
    // Token é válido
}

// Verifica se um token expirou
bool expired = _tokenService.IsTokenExpired(token);

// Obtém a data de expiração do token (DateTime?)
DateTime? expiration = _tokenService.GetTokenExpiration(token);

// Obtém o id do token (claim jti) de um token
string? tokenId = _tokenService.GetTokenIdFromJwt(token);
```

### Membros do ITokenService

| Membro | Descrição |
|--------|-----------|
| `GenerateToken(IEnumerable<Claim> claims)` | Gera um token JWT de acesso assinado |
| `GenerateRefreshToken(IEnumerable<Claim> claims)` | Gera um refresh token com tempo de vida maior |
| `ValidateToken(string token)` | Valida a assinatura e o tempo de vida do token; retorna o `ClaimsPrincipal` ou `null` |
| `GetTokenIdFromJwt(string token)` | Extrai o valor da claim `jti` |
| `IsTokenExpired(string token)` | Retorna `true` se o token já expirou |
| `GetTokenExpiration(string token)` | Retorna a data de expiração do token, ou `null` |

## Hash de Senhas (Argon2id)

O `IPasswordHashService` / `Argon2idPasswordHashService` faz o hash e a verificação de senhas usando o algoritmo **Argon2id** (vencedor da Password Hashing Competition).

### Configuração

Registre com os parâmetros padrão:

```csharp
using BlackDigital.AspNet.Security;

builder.Services.AddArgon2idPasswordHashService();
```

Ou customize os parâmetros do Argon2:

```csharp
builder.Services.AddArgon2idPasswordHashService(
    degreeOfParallelism: 4,     // número de threads
    iterations: 3,              // número de iterações
    memorySize: 65536           // memória em KiB (64 MB)
);
```

### Fazendo Hash e Verificando

```csharp
public class UserService
{
    private readonly IPasswordHashService _passwordHash;

    public UserService(IPasswordHashService passwordHash)
    {
        _passwordHash = passwordHash;
    }

    public void CreateUser(string email, string plainPassword)
    {
        string hash = _passwordHash.HashPassword(plainPassword);
        // Armazene o hash no banco de dados
    }

    public bool VerifyPassword(string plainPassword, string storedHash)
    {
        return _passwordHash.VerifyHashedPassword(storedHash, plainPassword);
    }
}
```

### Membros do IPasswordHashService

| Membro | Descrição |
|--------|-----------|
| `HashPassword(string password)` | Retorna o hash Argon2id da senha |
| `VerifyHashedPassword(string hash, string password)` | Retorna `true` se a senha corresponde ao hash |

## Documentação Relacionada

- [Documentação Principal](../README.pt.md)
