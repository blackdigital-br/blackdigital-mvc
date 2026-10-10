using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace BlackDigital.AspNet.Security
{
    public class JwtService : ITokenService
    {
        private readonly string _secretKey;
        private readonly string _issuer;
        private readonly string _audience;
        private readonly TimeSpan _defaultExpiration;

        public JwtService(string secretKey, string issuer, string audience, int expirationMinutes = 1440)
        {
            if (string.IsNullOrWhiteSpace(secretKey))
                throw new ArgumentException("Secret key cannot be null or empty.", nameof(secretKey));

            // HMAC-SHA256 requires a key of at least 256 bits (32 bytes). Enforce
            // it so callers don't accidentally weaken signing with short secrets.
            var keyBytes = Encoding.ASCII.GetBytes(secretKey);
            if (keyBytes.Length < 32)
                throw new ArgumentException("Secret key must be at least 32 bytes (256 bits) long for HMAC-SHA256.", nameof(secretKey));

            if (string.IsNullOrWhiteSpace(issuer))
                throw new ArgumentException("Issuer cannot be null or empty.", nameof(issuer));

            if (string.IsNullOrWhiteSpace(audience))
                throw new ArgumentException("Audience cannot be null or empty.", nameof(audience));

            if (expirationMinutes <= 0)
                throw new ArgumentOutOfRangeException(nameof(expirationMinutes), "Expiration must be greater than zero minutes.");

            _secretKey = secretKey;
            _issuer = issuer;
            _audience = audience;
            _defaultExpiration = TimeSpan.FromMinutes(expirationMinutes);
        }

        public string GenerateToken(Guid tokenId, IEnumerable<Claim> claims, TimeSpan? expiration = null)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_secretKey);

            var now = DateTime.UtcNow;
            var expirationSpan = expiration ?? _defaultExpiration;

            var expires = now.Add(expirationSpan);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(JwtRegisteredClaimNames.Jti, tokenId.ToString())
                }),
                Expires = expires,
                Issuer = _issuer,
                Audience = _audience,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            if (claims != null)
            {
                foreach (var claim in claims)
                {
                    tokenDescriptor.Subject.AddClaim(claim);
                }
            }

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        public ClaimsPrincipal? ValidateToken(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(_secretKey);

                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = _issuer,
                    ValidateAudience = true,
                    ValidAudience = _audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };

                var principal = tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);
                return principal;
            }
            catch
            {
                return null;
            }
        }

        public string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        public Guid? GetTokenIdFromJwt(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var jsonToken = tokenHandler.ReadJwtToken(token);

                var tokenIdClaim = jsonToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti);
                if (tokenIdClaim != null && Guid.TryParse(tokenIdClaim.Value, out var tokenId))
                {
                    return tokenId;
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        public bool IsTokenExpired(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var jsonToken = tokenHandler.ReadJwtToken(token);

                return jsonToken.ValidTo <= DateTime.UtcNow;
            }
            catch
            {
                return true;
            }
        }

        public DateTime GetTokenExpiration(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var jsonToken = tokenHandler.ReadJwtToken(token);

                return jsonToken.ValidTo;
            }
            catch
            {
                return DateTime.MinValue;
            }
        }
    }
}
