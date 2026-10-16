namespace Cine.Application.Dtos;

public class TmdbMovieDetailDto
{
    public int TmdbId { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string TituloOriginal { get; set; } = string.Empty;
    public string Sinopsis { get; set; } = string.Empty;
    public int Duracion { get; set; }
    public string Clasificacion { get; set; } = "B";
    public string Director { get; set; } = string.Empty;
    public string Actores { get; set; } = string.Empty;
    public string Genero { get; set; } = string.Empty;
    public string PosterPath { get; set; } = string.Empty; // Ruta relativa estricta ej: "/qmDpIHrmpJINaRKAfWQfftjCdyi.jpg"
    public string BackdropPath { get; set; } = string.Empty; // Ruta relativa estricta
    public string ReleaseYear { get; set; } = string.Empty;
    public double Rating { get; set; }
    public string? TrailerKey { get; set; }
}
