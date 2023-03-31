using BlackDigital.Mvc.Example.Services;
using BlackDigital.Mvc.Rest;
using BlackDigital.Rest;
using Microsoft.AspNetCore.Mvc;

namespace BlackDigital.Mvc.Example.Controllers
{
    [ApiController]
    [Microsoft.AspNetCore.Mvc.Route("[controller]")]
    public class TestController : BaseController<IUser>
    {
        public TestController(IUser entityController) : base(entityController)
        {
        }

        [HttpPost("{id}")]
        public async Task<ActionResult> Get([FromRoute] int id)
        {
            return await ExecuteActionAsync("SaveUserAsync", new Dictionary<string, object>
            {
                { "name", "name" },
                { "email", "email" },
                { "password", "password" }
            });
        }
    }
}
