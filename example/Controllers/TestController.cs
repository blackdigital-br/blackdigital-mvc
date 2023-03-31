using BlackDigital.Rest;
using Microsoft.AspNetCore.Mvc;

namespace BlackDigital.Mvc.Example.Controllers
{
    [ApiController]
    [Microsoft.AspNetCore.Mvc.Route("[controller]")]
    public class TestController : ControllerBase
    {
        [HttpPost]
        public ActionResult Get(string name,
                                string email,
                                string password)
        {
            return Ok("Test1");
        }
    }
}
