using Microsoft.AspNetCore.Http;

namespace BlackDigital.AspNet.Services
{
    /// <summary>
    /// Extension helpers over <see cref="IHttpContextAccessor"/> for common
    /// request-scoped lookups (client IP, JWT token id).
    /// </summary>
    /// <remarks>
    /// For the client IP to be trustworthy in production behind a reverse
    /// proxy, the host must register the <c>ForwardedHeaders</c> middleware
    /// with <c>ForwardedHeadersOptions.KnownProxies</c> /
    /// <c>KnownNetworks</c> populated. That middleware rewrites
    /// <see cref="ConnectionInfo.RemoteIpAddress"/> from the trusted
    /// <c>X-Forwarded-For</c> chain, which is what this helper prefers.
    /// </remarks>
    public static class BdHttpContextHelper
    {
        /// <summary>
        /// Returns the client's IP address. Prefers
        /// <see cref="ConnectionInfo.RemoteIpAddress"/> (set correctly by the
        /// <c>ForwardedHeaders</c> middleware), falling back to the first
        /// entry in <c>X-Forwarded-For</c> and then <c>X-Real-IP</c>.
        /// Returns <c>null</c> when no IP can be determined.
        /// </summary>
        /// <param name="httpContext">The HTTP context accessor to read the request from.</param>
        /// <returns>The client's IP address as a string, or <c>null</c> when it cannot be determined.</returns>
        /// <exception cref="InvalidOperationException">Thrown when <see cref="IHttpContextAccessor.HttpContext"/> is <c>null</c>.</exception>
        public static string? GetCurrentIpAddress(this IHttpContextAccessor httpContext)
        {
            var context = httpContext.HttpContext
                ?? throw new InvalidOperationException("HttpContext is not available.");

            // RemoteIpAddress is the most trustworthy source when the
            // ForwardedHeaders middleware has processed the request.
            var remoteIp = context.Connection.RemoteIpAddress?.ToString();
            if (!string.IsNullOrEmpty(remoteIp))
                return remoteIp;

            // X-Forwarded-For may contain a comma-separated chain; the
            // left-most entry is the original client IP.
            var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(forwardedFor))
            {
                var firstIp = forwardedFor.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                          .Select(ip => ip.Trim())
                                          .FirstOrDefault();
                if (!string.IsNullOrEmpty(firstIp))
                    return firstIp;
            }

            var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(realIp))
                return realIp;

            return null;
        }

        /// <summary>
        /// Returns the JWT <c>jti</c> (token id) claim from the current
        /// authenticated user, or null when not present.
        /// </summary>
        /// <param name="httpContext">The HTTP context accessor to read the user claims from.</param>
        /// <returns>The token id (<c>jti</c>) claim value, or <c>null</c> when not present.</returns>
        public static string? GetCurrentTokenIdentifier(this IHttpContextAccessor httpContext)
        {
            var claims = httpContext.HttpContext?.User?.Claims;

            if (claims == null)
            {
                return null;
            }

            var jtiClaim = claims.FirstOrDefault(c => c.Type == "jti");

            return jtiClaim?.Value;
        }
    }
}
