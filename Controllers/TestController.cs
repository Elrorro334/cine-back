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
            return Ok("Servicio funcionando en AZURE actualizado a las 9:40 am");
        }
    }
}
