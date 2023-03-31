using BlackDigital.Rest;

namespace BlackDigital.Mvc.Example.Services
{
    [Service("api/user")]
    public interface IUser
    {
        [Action(method: RestMethod.Get, authorize: false)]
        Task<string> GetUserAsync(string name);

        [Action(method: RestMethod.Post, authorize: false)]
        Task<User> SaveUserAsync(string name,
                                 string email,
                                 string password);
    }   
}
