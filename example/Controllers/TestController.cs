using BlackDigital.Model;
using BlackDigital.Mvc.Example.Services;
using BlackDigital.Mvc.Rest;
using BlackDigital.Rest;
using Microsoft.AspNetCore.Mvc;

namespace BlackDigital.Mvc.Example.Controllers
{
    [ApiController]
    [Microsoft.AspNetCore.Mvc.Route("[controller]")]
    public class TestController : Controller
    {
        public TestController(IUser entityController)
        {
            _entityController = entityController ?? throw new ArgumentNullException(nameof(entityController));
        }

        private readonly IUser _entityController;

        [HttpPost("{id:id}")]
        public async Task<ActionResult> Get([FromRoute] Id id)
        {
            var user = await _entityController.GetUserAsync(id.ToString());

            return Ok(user);
        }
    }
}
