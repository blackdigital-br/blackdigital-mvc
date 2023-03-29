namespace BlackDigital.Mvc.Example.Services
{
    public class UserImplemention : IUser
    {
        public Task<string> GetUserAsync()
        {
            BusinessException.ThrowNotFound();
            return Task.FromResult("User Name");
        }

        public Task<bool> SaveUserAsync(string name, string email, string password)
        {
            return Task.FromResult(true);
        }
    }
}
