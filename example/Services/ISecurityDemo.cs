using BlackDigital.Rest;

namespace BlackDigital.AspNet.Example.Services
{
    // The interface only describes routes. Access requirements ([Authorize])
    // are declared on SecurityDemoImplementation, never here, because the
    // interface may be shared with the front end.
    [Service("api/security")]
    public interface ISecurityDemo
    {
        [Action("public", method: RestMethod.Get)]
        Task<string> GetPublicAsync();

        [Action("authenticated", method: RestMethod.Get)]
        Task<string> GetAuthenticatedAsync();

        [Action("admin", method: RestMethod.Get)]
        Task<string> GetAdminAsync();

        [Action("policy-read", method: RestMethod.Get)]
        Task<string> GetPolicyReadAsync();

        [Action("policy-write", method: RestMethod.Get)]
        Task<string> GetPolicyWriteAsync();
    }
}
