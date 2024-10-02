using BlackDigital.Model;
using BlackDigital.Rest;

namespace BlackDigital.Mvc.Example.Services
{
    [Service("api/user")]
    public interface IUser
    {
        [Action("{name}", method: RestMethod.Get, authorize: false)]
        Task<string> GetUserAsync([Route] string name);

        [Action(method: RestMethod.Post, authorize: false)]
        Task<Id> SaveUserAsync(string name,
                                 string email,
                                 string password);

        [Action("{id}", method: RestMethod.Put, authorize: false)]
        Task<Id> UpdateUserAsync([Route] Id id, 
                                    string name,
                                    string email);

        [Action("{id}", method: RestMethod.Delete, authorize: false)]
        Task<int> DeleteUserAsync([Route] Id id);
    }   
}
