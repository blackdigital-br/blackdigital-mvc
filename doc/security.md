# Security

[🇧🇷 Português](security.pt.md) | 🇺🇸 English

The Security module provides two ready-to-use building blocks: a **JWT token service** and an **Argon2id password hashing service**.

## JWT Token Service

The `ITokenService` / `JwtService` handles JWT generation, validation, and inspection.

### Configuration

The service is configured via the `Jwt` section in `appsettings.json`:

```json
{
  "Jwt": {
    "SecretKey": "your-very-long-secret-key-at-least-32-chars",
    "Issuer": "https://your-app.com",
    "Audience": "https://your-app.com",
    "AccessTokenExpirationMinutes": 60,
    "RefreshTokenExpirationDays": 30
  }
}
```

Register it in `Program.cs`:

```csharp
using BlackDigital.AspNet.Security;

builder.Services.AddJwtService(builder.Configuration);
```

Alternatively, configure it inline without a `Jwt` config section:

```csharp
builder.Services.AddJwtService(
    secretKey: "your-very-long-secret-key-at-least-32-chars",
    issuer: "https://your-app.com",
    audience: "https://your-app.com",
    accessTokenExpirationMinutes: 60,
    refreshTokenExpirationDays: 30
);
```

### Generating Tokens

Inject `ITokenService` and generate access and refresh tokens:

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

### Validating and Inspecting Tokens

```csharp
// Validate a token and get the claims principal
ClaimsPrincipal? principal = _tokenService.ValidateToken(token);

if (principal != null)
{
    // Token is valid
}

// Check whether a token has expired
bool expired = _tokenService.IsTokenExpired(token);

// Get the token's expiration date (DateTime?)
DateTime? expiration = _tokenService.GetTokenExpiration(token);

// Get the token id (jti claim) from a token
string? tokenId = _tokenService.GetTokenIdFromJwt(token);
```

### ITokenService Members

| Member | Description |
|--------|-------------|
| `GenerateToken(IEnumerable<Claim> claims)` | Generates a signed JWT access token |
| `GenerateRefreshToken(IEnumerable<Claim> claims)` | Generates a refresh token with a longer lifetime |
| `ValidateToken(string token)` | Validates the token signature and lifetime; returns the `ClaimsPrincipal` or `null` |
| `GetTokenIdFromJwt(string token)` | Extracts the `jti` claim value |
| `IsTokenExpired(string token)` | Returns `true` if the token is past its expiration |
| `GetTokenExpiration(string token)` | Returns the token's expiration date, or `null` |

## Password Hashing (Argon2id)

The `IPasswordHashService` / `Argon2idPasswordHashService` hashes and verifies passwords using the **Argon2id** algorithm (winner of the Password Hashing Competition).

### Configuration

Register with default parameters:

```csharp
using BlackDigital.AspNet.Security;

builder.Services.AddArgon2idPasswordHashService();
```

Or customise the Argon2 parameters:

```csharp
builder.Services.AddArgon2idPasswordHashService(
    degreeOfParallelism: 4,     // number of threads
    iterations: 3,              // number of iterations
    memorySize: 65536           // memory in KiB (64 MB)
);
```

### Hashing and Verifying

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
        // Store hash in the database
    }

    public bool VerifyPassword(string plainPassword, string storedHash)
    {
        return _passwordHash.VerifyHashedPassword(storedHash, plainPassword);
    }
}
```

### IPasswordHashService Members

| Member | Description |
|--------|-------------|
| `HashPassword(string password)` | Returns the Argon2id hash of the password |
| `VerifyHashedPassword(string hash, string password)` | Returns `true` if the password matches the hash |

## Related Documentation

- [Main Documentation](../README.md)
