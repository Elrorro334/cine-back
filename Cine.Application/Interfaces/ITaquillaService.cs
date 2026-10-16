using Cine.Application.Dtos;

namespace Cine.Application.Interfaces
{
    public interface ITaquillaService
    {
        Task<List<PeliculaCarteleraDto>> ObtenerCarteleraHoyAsync(string? busqueda = null);
        Task<List<TmdbSearchResultDto>> BuscarTmdbAsync(string query);
        Task<TmdbMovieDetailDto?> ObtenerDetalleTmdbAsync(int tmdbId);
        Task<PeliculaCarteleraDto> RegistrarPeliculaDesdeTmdbAsync(RegistrarPeliculaDto dto);
        Task<bool> EliminarPeliculaAsync(int idPelicula);
    }
}
