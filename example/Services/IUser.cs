using BlackDigital.Rest;

namespace BlackDigital.Mvc.Example.Services
{
    [Service("api/user")]
    public interface IUser
    {
        [Action(method: RestMethod.Get)]
        Task<string> GetUserAsync();

        [Action(method: RestMethod.Post)]
        Task<bool> SaveUserAsync([FromBody] string name,
                                 [FromBody] string email,
                                 [FromBody] string password);
    }
}
