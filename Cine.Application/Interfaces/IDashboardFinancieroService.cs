using Cine.Application.Dtos;

namespace Cine.Application.Interfaces;

public interface IDashboardFinancieroService
{
    /// <summary>
    /// Obtiene el resumen del dashboard financiero incluyendo los 4 KPIs clave y las transacciones detalladas.
    /// </summary>
    Task<DashboardFinancieroDto> ObtenerDashboardAsync(FiltroDashboardDto filtro);

    /// <summary>
    /// Retorna las fechas y descripción de la semana cinematográfica actual (Jueves a Miércoles).
    /// </summary>
    Task<SemanaCinematograficaDto> ObtenerSemanaActualAsync();

    /// <summary>
    /// Genera un archivo CSV codificado en UTF-8 con BOM con el histórico de transacciones del periodo.
    /// </summary>
    Task<byte[]> ExportarTransaccionesCsvAsync(FiltroDashboardDto filtro);
}
