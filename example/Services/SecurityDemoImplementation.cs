using BlackDigital.Domain.Security;

namespace BlackDigital.AspNet.Example.Services
{
    public class SecurityDemoImplementation : ISecurityDemo
    {
        // No requirement → anyone can call GET /api/security/public.
        public Task<string> GetPublicAsync()
            => Task.FromResult("Public: no [Authorize] on the implementation.");

        // Requirement with no Roles/Policy → any authenticated user.
        [Authorize]
        public Task<string> GetAuthenticatedAsync()
            => Task.FromResult("Authenticated: any valid token can call this.");

        // Roles → the user must hold at least one of the listed roles.
        [Authorize(Roles = new[] { "Admin" })]
        public Task<string> GetAdminAsync()
            => Task.FromResult("Roles: only the 'Admin' role.");

        // Policy → evaluated through IAuthorizationService ("Users.Read" = Admin or Manager).
        [Authorize(Policy = "Users.Read")]
        public Task<string> GetPolicyReadAsync()
            => Task.FromResult("Policy 'Users.Read': Admin or Manager.");

        // Policy → ("Users.Write" = Admin only).
        [Authorize(Policy = "Users.Write")]
        public Task<string> GetPolicyWriteAsync()
            => Task.FromResult("Policy 'Users.Write': Admin only.");
    }
}
