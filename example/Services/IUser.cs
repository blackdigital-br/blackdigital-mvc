using BlackDigital.AspNet.Example.Models;
using BlackDigital.Rest;

namespace BlackDigital.AspNet.Example.Services
{
    [Service("api/user")]
    public interface IUser
    {
        [Action("{name}", method: RestMethod.Get, authorize: false)]
        Task<string> GetUserAsync([Path] string name);

        [Action(method: RestMethod.Post, authorize: false)]
        Task<SaveUser> SaveUserAsync([Body] SaveUser user);
    }   
}
