using BlackDigital.AspNet.Example.Models;
using BlackDigital.AspNet.Example.Transform;
using BlackDigital.AspNet.Rest.Transforms;
using BlackDigital.Rest;

namespace BlackDigital.AspNet.Example.Services
{
    [Service("api/user")]
    public interface IUser
    {
        [Action("{name}", method: RestMethod.Get)]
        Task<string> GetUserAsync([Path] string name);

        [Action(method: RestMethod.Post)]
        [BlackDigital.AspNet.Rest.Transform(typeof(OldSaveUsertoSaveUserTransformRule), "2020-10-10", TransformDirection.Input)]
        Task<SaveUser> SaveUserAsync([Body] SaveUser user);
    }   
}
