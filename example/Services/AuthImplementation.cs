using BlackDigital.AspNet.Example.Models;
using BlackDigital.AspNet.Services;
using BlackDigital.Domain.Security;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace BlackDigital.AspNet.Example.Services
{
    public class AuthImplementation : IAuth
    {
        // Demo accounts only — replace with a real user store (and hashed
        // passwords through IPasswordHashService) in your own project.
        private static readonly Dictionary<string, (string Password, string[] Roles)> Accounts =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["admin"] = ("admin", new[] { "Admin" }),
                ["manager"] = ("manager", new[] { "Manager" }),
                ["user"] = ("user", Array.Empty<string>())
            };

        private readonly ITokenService _tokenService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuthImplementation(ITokenService tokenService, IHttpContextAccessor httpContextAccessor)
        {
            _tokenService = tokenService;
            _httpContextAccessor = httpContextAccessor;
        }

        public Task<TokenResponse> LoginAsync(LoginRequest request)
        {
            if (!Accounts.TryGetValue(request.User, out var account) || account.Password != request.Password)
                BusinessException.Throw("Invalid user or password.", StatusCodes.Status401Unauthorized);

            var claims = new List<Claim> { new(ClaimTypes.Name, request.User) };
            claims.AddRange(account.Roles.Select(role => new Claim(ClaimTypes.Role, role)));

            var token = _tokenService.GenerateToken(Guid.NewGuid(), claims, null);
            var expiresIn = (_tokenService.GetTokenExpiration(token) - DateTime.UtcNow).TotalSeconds;

            return Task.FromResult(new TokenResponse
            {
                AccessToken = token,
                ExpiresInSeconds = expiresIn > 0 ? (int)expiresIn : 0
            });
        }

        // [Authorize] is declared on the implementation, never on the interface.
        [Authorize]
        public Task<Dictionary<string, string?>> GetCurrentUserAsync()
        {
            var user = _httpContextAccessor.HttpContext?.User;

            var result = new Dictionary<string, string?>
            {
                ["name"] = user?.Identity?.Name,
                ["tokenId"] = _httpContextAccessor.GetCurrentTokenIdentifier(),
                ["roles"] = string.Join(", ", user?.FindAll(ClaimTypes.Role).Select(claim => claim.Value) ?? [])
            };

            return Task.FromResult(result);
        }
    }
}
