using BlackDigital.AspNet.Example.Models;
using BlackDigital.Rest;

namespace BlackDigital.AspNet.Example.Services
{
    [Service("api/auth")]
    public interface IAuth
    {
        // Public: the implementation does not declare [Authorize] on this action.
        [Action("login", method: RestMethod.Post)]
        Task<TokenResponse> LoginAsync([Body] LoginRequest request);

        // Secured: the implementation declares [Authorize] on this action.
        [Action("me", method: RestMethod.Get)]
        Task<Dictionary<string, string?>> GetCurrentUserAsync();
    }
}
