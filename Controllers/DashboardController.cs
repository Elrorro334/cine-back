using Cine.Application.Dtos;
using Cine.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace cine_back.Controllers;

[Authorize(Roles = "Administrador,Admin")]
[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly IDashboardFinancieroService _dashboardService;

    public DashboardController(IDashboardFinancieroService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    /// <summary>
    /// Criterios 1 y 2: Obtiene el resumen financiero con los 4 KPIs clave y las transacciones detalladas.
    /// Soporta selector de Semana Cinematográfica (offsetSemanas) o rango de fechas personalizado.
    /// </summary>
    [HttpGet("resumen")]
    public async Task<ActionResult<DashboardFinancieroDto>> GetResumen(
        [FromQuery] DateTime? fechaInicio = null,
        [FromQuery] DateTime? fechaFin = null,
        [FromQuery] int? offsetSemanas = null)
    {
        var filtro = new FiltroDashboardDto
        {
            FechaInicio = fechaInicio,
            FechaFin = fechaFin,
            OffsetSemanas = offsetSemanas
        };

        var resultado = await _dashboardService.ObtenerDashboardAsync(filtro);
        return Ok(resultado);
    }

    /// <summary>
    /// Obtiene el rango de fechas de la semana cinematográfica actual (Jueves a Miércoles).
    /// </summary>
    [HttpGet("semana-actual")]
    public async Task<ActionResult<SemanaCinematograficaDto>> GetSemanaActual()
    {
        var resultado = await _dashboardService.ObtenerSemanaActualAsync();
        return Ok(resultado);
    }

    /// <summary>
    /// Criterio 3: Exporta el listado de transacciones a formato CSV estructurado para Excel / Contabilidad.
    /// </summary>
    [HttpGet("exportar-transacciones")]
    public async Task<IActionResult> ExportarTransacciones(
        [FromQuery] DateTime? fechaInicio = null,
        [FromQuery] DateTime? fechaFin = null,
        [FromQuery] int? offsetSemanas = null)
    {
        var filtro = new FiltroDashboardDto
        {
            FechaInicio = fechaInicio,
            FechaFin = fechaFin,
            OffsetSemanas = offsetSemanas
        };

        var bytes = await _dashboardService.ExportarTransaccionesCsvAsync(filtro);
        string nombreArchivo = $"reporte_financiero_cine_xilotzin_{DateTime.Now:yyyyMMdd_HHmmss}.csv";

        return File(bytes, "text/csv; charset=utf-8", nombreArchivo);
    }
}
