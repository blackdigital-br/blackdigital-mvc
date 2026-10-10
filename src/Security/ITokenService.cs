using System.Security.Claims;

namespace BlackDigital.AspNet.Security
{
    public interface ITokenService
    {
        string GenerateToken(Guid tokenId, IEnumerable<Claim> claims, TimeSpan? expiration = null);
        ClaimsPrincipal? ValidateToken(string token);
        string GenerateRefreshToken();
        Guid? GetTokenIdFromJwt(string token);
        bool IsTokenExpired(string token);
        DateTime GetTokenExpiration(string token);
    }
}
