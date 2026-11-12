using System.Collections.Generic;
using System.Threading.Tasks;
using Cine.Application.Dtos;

namespace Cine.Application.Interfaces;

public interface IConfiguracionAdminService
{
    Task<ConfiguracionSeguridadDto> ObtenerConfiguracionSeguridadAsync(string clientIp);
    Task<ConfiguracionSeguridadDto> ActualizarConfiguracionSeguridadAsync(
        ActualizarConfiguracionSeguridadDto dto,
        string clientIp,
        int usuarioModificoId);
    Task<List<TematicaSitioDto>> ObtenerTematicasAsync();
    Task<TematicaSitioDto?> ObtenerTematicaActivaAsync();
    Task<TematicaSitioDto> ActivarTematicaAsync(int tematicaId);
}
