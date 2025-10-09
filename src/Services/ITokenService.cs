
using System.Security.Claims;

namespace BlackDigital.AspNet.Services
{
    public interface ITokenService
    {
        string GenerateToken(Guid tokenId, int expirationMinutes = 15);
        ClaimsPrincipal? ValidateToken(string token);
        string GenerateRefreshToken();
        Guid? GetTokenIdFromJwt(string token);
        bool IsTokenExpired(string token);
        DateTime GetTokenExpiration(string token);
    }
}
