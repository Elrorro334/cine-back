using Cine.Application.Dtos;
using Cine.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace cine_back.Controllers;

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

    /// <remarks>
    /// **Ejemplo para probar en Swagger:**
    /// ```text
    /// busqueda: spider   (o dejar vacío para traer toda la cartelera del día)
    /// ```
    /// </remarks>
    [HttpGet("cartelera")]
    public async Task<IActionResult> GetCartelera([FromQuery] string? busqueda)
    {
        var cartelera = await _taquillaService.ObtenerCarteleraHoyAsync(busqueda);
        return Ok(cartelera);
    }

    /// <remarks>
    /// **Ejemplo para probar en Swagger:**
    /// ```text
    /// idFuncion: 1
    /// ```
    /// </remarks>
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

    /// <remarks>
    /// **Ejemplo para probar en Swagger:**
    /// ```text
    /// Presiona "Try it out" y luego "Execute" (no requiere parámetros).
    /// ```
    /// </remarks>
    [HttpGet("dulceria")]
    public async Task<IActionResult> GetDulceria()
    {
        var productos = await _taquillaService.ObtenerProductosDulceriaAsync();
        return Ok(productos);
    }

    /// <remarks>
    /// **Ejemplo para probar en Swagger (HU-07):**
    /// ```text
    /// Presiona "Try it out" y luego "Execute" (no requiere parámetros).
    /// ```
    /// </remarks>
    [HttpGet("dulceria/catalogo-pos")]
    public async Task<IActionResult> GetCatalogoDulceriaPos()
    {
        var catalogo = await _taquillaService.ObtenerCatalogoDulceriaPosAsync();
        return Ok(catalogo);
    }

    /// <remarks>
    /// **Ejemplo para probar en Swagger (pega cualquiera de estos SKU en el parámetro `sku`):**
    /// ```text
    /// 7501001  -> Palomitas Grandes ($50.00)
    /// 7501002  -> Refresco 600ml ($35.00)
    /// 7501003  -> Nachos con Queso ($65.00)
    /// 7501004  -> Chips / Frituras ($25.00)
    /// 7501005  -> Icee / Granizado ($45.00)
    /// 7501006  -> Rebanada Individual ($35.00)
    /// 7501007  -> Pizza Completa 8 Rebanadas ($180.00)
    /// ```
    /// </remarks>
    [HttpGet("dulceria/sku/{sku}")]
    public async Task<IActionResult> BuscarProductoPorSku(string sku)
    {
        try
        {
            var producto = await _taquillaService.BuscarProductoPorSkuAsync(sku);
            if (producto == null)
            {
                return NotFound(new { error = $"No se encontró ningún producto con el código de barras (SKU) '{sku}'." });
            }
            return Ok(producto);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    /// <remarks>
    /// **Cómo probar en Swagger (pega este JSON en el Request body):**
    /// ```json
    /// {
    ///   "itemsActuales": [],
    ///   "skuAgregar": "7501006",
    ///   "idProductoAgregar": null,
    ///   "cantidadAgregar": 1
    /// }
    /// ```
    /// </remarks>
    [HttpPost("dulceria/calcular-orden")]
    public async Task<IActionResult> CalcularOrdenPos([FromBody] CalcularOrdenRequestDto request)
    {
        try
        {
            var resumen = await _taquillaService.CalcularOrdenPosAsync(request);
            return Ok(resumen);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    /// <remarks>
    /// **Cómo probar en Swagger (pega este JSON en el Request body):**
    /// ```json
    /// {
    ///   "idVendedor": 1,
    ///   "metodoPago": "Efectivo",
    ///   "items": [
    ///     {
    ///       "sku": "7501001",
    ///       "nombre": "Palomitas Grandes",
    ///       "tipo": "dulceria",
    ///       "cantidad": 1,
    ///       "precio": 50
    ///     },
    ///     {
    ///       "sku": "7501006",
    ///       "nombre": "Rebanada Individual",
    ///       "tipo": "dulceria",
    ///       "cantidad": 1,
    ///       "precio": 35
    ///     }
    ///   ]
    /// }
    /// ```
    /// </remarks>
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
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    /// <remarks>
    /// **Ejemplo para probar en Swagger:**
    /// ```text
    /// codigoQr: PRE-XIL-001
    /// ```
    /// </remarks>
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
