using Microsoft.AspNetCore.Mvc;

namespace cine_back.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Route("api/[controller]")]
    public class TestController : ControllerBase
    {
        [HttpGet]
        public ActionResult<string> Get()
        {
            return Ok("Servicio funcionando");
        }
    }
}
