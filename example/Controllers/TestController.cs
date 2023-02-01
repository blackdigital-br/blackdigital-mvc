using Microsoft.AspNetCore.Mvc;

namespace BlackDigital.Mvc.Example.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TestController : ControllerBase
    {
        [HttpGet]
        public ActionResult Get()
        {
            return Ok("Test1");
        }
    }
}
