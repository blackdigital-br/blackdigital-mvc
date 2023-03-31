namespace BlackDigital.Mvc.Example.Services
{
    public class UserImplemention : IUser
    {
        public Task<string> GetUserAsync(string name)
        {
            BusinessException.ThrowNotFound();
            return Task.FromResult("User Name");
        }

        public Task<User> SaveUserAsync(string name, string email, string password)
        {
            return Task.FromResult(new User
            {
                Email = "a@a.com",
                Name = "User Name"
            });
        }
    }
}
