using BlackDigital.Model;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BlackDigital.Mvc.Example.Services
{
    public class UserImplemention : IUser
    {
        public Task<string> GetUserAsync(string name)
        {
            //BusinessException.ThrowNotFound();
            return Task.FromResult("User Name");
        }

        public Task<Id> SaveUserAsync(string name, string email, string password)
        {
            return Task.FromResult((Id)12);
            /*return Task.FromResult(new User
            {
                Email = "a@a.com",
                Name = "User Name"
            });*/
        }

        public Task<Id> UpdateUserAsync(Id id, string name, string email)
        {
            return Task.FromResult((Id)12);
        }

        public Task<int> DeleteUserAsync(Id id)
        {
            return Task.FromResult(1);
        }
    }
}
