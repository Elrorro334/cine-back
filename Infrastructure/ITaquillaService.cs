using cine_back.Dtos;

namespace cine_back.Infrastructure
{
    public interface ITaquillaService
    {
        Task<List<PeliculaCarteleraDto>> ObtenerCarteleraHoyAsync(string? busqueda = null);
    }
}
