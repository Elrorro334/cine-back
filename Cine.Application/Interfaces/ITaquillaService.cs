using Cine.Application.Dtos;

namespace Cine.Application.Interfaces
{
    public interface ITaquillaService
    {
        Task<List<PeliculaCarteleraDto>> ObtenerCarteleraHoyAsync(string? busqueda = null);
    }
}
