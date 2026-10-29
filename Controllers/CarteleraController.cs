using Cine.Application.Dtos;
using Cine.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace cine_back.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CarteleraController : ControllerBase
{
    private readonly ITaquillaService _taquillaService;

    public CarteleraController(ITaquillaService taquillaService)
    {
        _taquillaService = taquillaService;
    }

    // Endpoint público: cartelera del día de hoy
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> GetCartelera([FromQuery] string? busqueda)
    {
        var cartelera = await _taquillaService.ObtenerCarteleraHoyAsync(busqueda);
        return Ok(cartelera);
    }

    // Endpoint público: promociones y avisos dinámicos
    [AllowAnonymous]
    [HttpGet("trailer/{idPelicula:int}")]
    public async Task<IActionResult> ObtenerTrailerPelicula(int idPelicula)
    {
        var trailer = await _taquillaService.ObtenerTrailerPeliculaAsync(idPelicula);
        if (trailer == null)
        {
            return NotFound("No se encontró el tráiler para esta película.");
        }
        return Ok(trailer);
    }

    [AllowAnonymous]
    [HttpGet("promociones")]
    public async Task<IActionResult> GetPromociones()
    {
        var promociones = await _taquillaService.ObtenerPromocionesAsync();
        return Ok(promociones);
    }

    // Endpoint administrativo: búsqueda en TMDB
    [Authorize]
    [HttpGet("buscar-tmdb")]
    public async Task<IActionResult> BuscarTmdb([FromQuery] string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return BadRequest("El término de búsqueda no puede estar vacío.");
        }

        var resultados = await _taquillaService.BuscarTmdbAsync(query);
        return Ok(resultados);
    }

    // Endpoint administrativo: detalle completo de TMDB
    [Authorize]
    [HttpGet("detalle-tmdb/{id:int}")]
    public async Task<IActionResult> DetalleTmdb(int id)
    {
        var detalle = await _taquillaService.ObtenerDetalleTmdbAsync(id);
        if (detalle == null)
        {
            return NotFound("No se encontró la película en TMDB.");
        }
        return Ok(detalle);
    }

    // Endpoint administrativo: registrar película en SQL Server fijando funciones en rango
    [Authorize]
    [HttpPost("registrar")]
    public async Task<IActionResult> RegistrarPelicula([FromBody] RegistrarPeliculaDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Titulo))
        {
            return BadRequest("El título de la película es obligatorio.");
        }

        var pelicula = await _taquillaService.RegistrarPeliculaDesdeTmdbAsync(dto);
        return Created($"/api/cartelera/{pelicula.IdPelicula}", pelicula);
    }

    // Endpoint administrativo: sincronizar / inicializar catálogo base de películas desde TMDB
    [Authorize]
    [HttpPost("sincronizar-catalogo-base")]
    public async Task<IActionResult> SincronizarCatalogoBase()
    {
        var cartelera = await _taquillaService.SincronizarCatalogoBaseAsync();
        return Ok(cartelera);
    }

    // Endpoint administrativo: dar de baja / eliminar película de cartelera
    [Authorize]
    [HttpDelete("pelicula/{id:int}")]
    public async Task<IActionResult> EliminarPelicula(int id)
    {
        var eliminada = await _taquillaService.EliminarPeliculaAsync(id);
        if (!eliminada)
        {
            return NotFound("No se encontró la película para eliminar.");
        }

        return Ok(new { success = true, message = "Película retirada de cartelera exitosamente." });
    }

    // Endpoint público: generar pre-orden digital QR
    [AllowAnonymous]
    [HttpPost("preorden")]
    public async Task<IActionResult> CrearPreorden([FromBody] CrearPreordenDto dto)
    {
        try
        {
            var preorden = await _taquillaService.CrearPreordenAsync(dto);
            return Created($"/api/cartelera/preorden/{preorden.Codigo}", preorden);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}