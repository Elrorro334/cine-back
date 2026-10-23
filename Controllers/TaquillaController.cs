using Cine.Application.Dtos;
using Cine.Application.Interfaces;
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
        return Ok(cartelera);
    }

    // Endpoint: GET /api/taquilla/funciones/{idFuncion}/butacas
    [HttpGet("funciones/{idFuncion:int}/butacas")]
    public async Task<IActionResult> GetButacasFuncion(int idFuncion)
    {
        try
        {
            var butacas = await _taquillaService.ObtenerButacasFuncionAsync(idFuncion);
            return Ok(butacas);
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    // Endpoint: GET /api/taquilla/dulceria
    [HttpGet("dulceria")]
    public async Task<IActionResult> GetDulceria()
    {
        var productos = await _taquillaService.ObtenerProductosDulceriaAsync();
        return Ok(productos);
    }

    // Endpoint: POST /api/taquilla/venta
    [HttpPost("venta")]
    public async Task<IActionResult> RegistrarVenta([FromBody] RegistrarVentaDto dto)
    {
        try
        {
            var venta = await _taquillaService.RegistrarVentaAsync(dto);
            return Ok(venta);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    // Endpoint: GET /api/taquilla/preorden/{codigoQr}
    [HttpGet("preorden/{codigoQr}")]
    public async Task<IActionResult> GetPreorden(string codigoQr)
    {
        var preorden = await _taquillaService.ObtenerPreordenAsync(codigoQr);
        if (preorden == null)
        {
            return NotFound(new { error = "Pre-orden no encontrada o código QR inválido." });
        }
        return Ok(preorden);
    }
}