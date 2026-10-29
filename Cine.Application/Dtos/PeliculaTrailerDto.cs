namespace Cine.Application.Dtos
{
    public class PeliculaTrailerDto
    {
        public int IdPelicula { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string? TrailerKey { get; set; }
        public List<TrailerItemDto> Trailers { get; set; } = new();
    }

    public class TrailerItemDto
    {
        public string Key { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Tipo { get; set; } = string.Empty;
    }
}
