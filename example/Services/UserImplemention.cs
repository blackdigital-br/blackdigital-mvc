using BlackDigital.AspNet.Example.Models;

namespace BlackDigital.AspNet.Example.Services
{
    public class UserImplemention : IUser
    {
        public Task<string> GetUserAsync(string name)
        {
            //BusinessException.ThrowNotFound();
            return Task.FromResult("User Name");
        }

        public Task<SaveUser> SaveUserAsync(SaveUser user)
        {
            user.Id = 112;

            return Task.FromResult(user);
        }
    }
}
