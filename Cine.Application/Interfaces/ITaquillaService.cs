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
        Task<List<PeliculaCarteleraDto>> SincronizarCatalogoBaseAsync();
        Task<List<PromocionDto>> ObtenerPromocionesAsync();
        Task<FuncionButacasDto> ObtenerButacasFuncionAsync(int idFuncion);
        Task<List<ProductoDto>> ObtenerProductosDulceriaAsync();
        Task<VentaConfirmadaDto> RegistrarVentaAsync(RegistrarVentaDto dto);
        Task<PreordenDto?> ObtenerPreordenAsync(string codigoQr);
        Task<PreordenDto> CrearPreordenAsync(CrearPreordenDto dto);
        Task<PeliculaTrailerDto?> ObtenerTrailerPeliculaAsync(int idPelicula);
    }
}
