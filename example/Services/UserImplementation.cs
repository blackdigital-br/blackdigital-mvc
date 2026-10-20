using BlackDigital.AspNet.Example.Models;
using BlackDigital.Domain.Security;

namespace BlackDigital.AspNet.Example.Services
{
    public class UserImplementation : IUser
    {
        public Task<string> GetUserAsync(string name)
        {
            //BusinessException.ThrowNotFound();
            return Task.FromResult("User Name");
        }

        // Access requirements are declared on the implementation (never on the
        // interface). Use [Authorize] for any authenticated user, or narrow it
        // with Roles/Policy, e.g. [Authorize(Roles = new[] { "Admin" })].
        [Authorize]
        public Task<SaveUser> SaveUserAsync(SaveUser user)
        {
            user.Id = 112;

            return Task.FromResult(user);
        }
    }
}
