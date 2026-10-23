namespace Cine.Application.Dtos;

public class TmdbSearchResultDto
{
    public int TmdbId { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string TituloOriginal { get; set; } = string.Empty;
    public string Sinopsis { get; set; } = string.Empty;
    public string PosterPath { get; set; } = string.Empty; // Ruta relativa estricta ej: "/qmDpIHrmpJINaRKAfWQfftjCdyi.jpg"
    public string ReleaseYear { get; set; } = string.Empty;
    public double Rating { get; set; }
}
