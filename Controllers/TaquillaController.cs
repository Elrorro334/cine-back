using cine_back.Dtos;
using cine_back.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace cine_back.Controllers;

// [Authorize] garantiza que nadie sin un JWT válido pueda ver la cartelera del POS
 [Authorize]
[Route("api/[controller]")]
[ApiController]
public class TaquillaController : ControllerBase
{
    private readonly ITaquillaService _taquillaService;

    public TaquillaController(ITaquillaService taquillaService)
    {
        _taquillaService = taquillaService;
    }

    // Endpoint: GET /api/taquilla/cartelera?busqueda=spider
    [HttpGet("cartelera")]
    public async Task<IActionResult> GetCartelera([FromQuery] string? busqueda)
    {
        var cartelera = await _taquillaService.ObtenerCarteleraHoyAsync(busqueda);

        // Retornamos 200 OK junto con el JSON estructurado para pintar las tarjetas
        return Ok(cartelera);
    }
}