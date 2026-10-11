# Security Reference

## JWT token service

`JwtService` implements `ITokenService` and provides JWT generation, validation, and inspection.

### Registration

```csharp
// From configuration (reads the "Jwt" section)
builder.Services.AddJwtService(builder.Configuration);

// Or with explicit settings
builder.Services.AddJwtService(secretKey, issuer, audience, expirationMinutes: 1440);
```

The `IConfiguration` overload reads:
- `Jwt:SecretKey`
- `Jwt:Issuer`
- `Jwt:Audience`
- `Jwt:ExpirationMinutes` (defaults to `1440`)

It **fails fast** at registration time with `InvalidOperationException` if `SecretKey`, `Issuer`, or `Audience` are missing.

### Constructor constraints

- `secretKey` must be at least **32 bytes (256 bits)** — `ArgumentException` otherwise (HMAC-SHA256 requirement).
- `issuer` and `audience` cannot be null/empty.
- `expirationMinutes` must be greater than zero.

### API

| Method | Description |
|--------|-------------|
| `GenerateToken(Guid tokenId, IEnumerable<Claim> claims, TimeSpan? expiration)` | Creates a signed JWT; the `jti` claim is set from `tokenId`; defaults to the configured expiration |
| `ValidateToken(string token)` | Returns a `ClaimsPrincipal` or `null` on failure |
| `GenerateRefreshToken()` | Returns a 32-byte random base64 string |
| `GetTokenIdFromJwt(string token)` | Extracts the `jti` claim as a `Guid?` |
| `IsTokenExpired(string token)` | Returns `true` if `ValidTo <= DateTime.UtcNow` (or on read failure) |
| `GetTokenExpiration(string token)` | Returns `ValidTo` (or `DateTime.MinValue` on failure) |

### Validation parameters

`ValidateToken` uses:
- `ValidateIssuerSigningKey = true` (symmetric key from the secret)
- `ValidateIssuer = true`, `ValidIssuer` = configured issuer
- `ValidateAudience = true`, `ValidAudience` = configured audience
- `ValidateLifetime = true`
- `ClockSkew = TimeSpan.Zero`

Signing algorithm: `HmacSha256Signature`.

## Argon2id password hashing

`Argon2idPasswordHashService` implements `IPasswordHashService`.

### Registration

```csharp
// Default parameters
builder.Services.AddArgon2idPasswordHashService();

// Custom parameters
builder.Services.AddArgon2idPasswordHashService(
    memorySize: 65536,
    iterations: 3,
    parallelism: 1,
    saltLength: 16,
    hashLength: 32);
```

### Default parameters

| Parameter | Default | Minimum |
|-----------|---------|---------|
| `memorySize` | `65536` KB | `8` KB |
| `iterations` | `3` | `1` |
| `parallelism` | `1` | `1` |
| `saltLength` | `16` bytes | `8` bytes |
| `hashLength` | `32` bytes | `4` bytes |

### Hash format

`HashPassword` returns a self-describing Modular Crypt Format string:

```
$argon2id$v=19$m=65536,t=3,p=1$<base64-salt>$<base64-hash>
```

`VerifyPassword` parses this format, recomputes the hash with the stored parameters, and compares using `CryptographicOperations.FixedTimeEquals` to prevent timing attacks. It returns `false` for malformed hashes, mismatched versions, or wrong passwords.

## DI helper overview

`ServiceHelper` also provides generic registration helpers:

```csharp
services.AddTokenService<TTokenService>();      // ITokenService
services.AddPasswordService<TPasswordService>(); // IPasswordHashService
services.AddEmailTemplate<TTemplateEmailService>(); // ITemplateEmailService

// All three at once
services.ConfigServices<TTemplateEmailService, TTokenService, TPasswordService>();
```

## BdHttpContextHelper

Extension methods on `IHttpContextAccessor`:

- `GetCurrentIpAddress()` — prefers `Connection.RemoteIpAddress` (set by `ForwardedHeaders` middleware), falls back to the first `X-Forwarded-For` entry, then `X-Real-IP`.
- `GetCurrentTokenIdentifier()` — returns the `jti` claim from the current authenticated user.

For the client IP to be trustworthy behind a reverse proxy, the host must register the `ForwardedHeaders` middleware with `KnownProxies` / `KnownNetworks` populated.

## Source files

- `src/Security/JwtService.cs`
- `src/Security/ITokenService.cs`
- `src/Security/Argon2idPasswordHashService.cs`
- `src/Security/IPasswordHashService.cs`
- `src/Services/ServiceHelper.cs`
- `src/Services/BdHttpContextHelper.cs`
