namespace Cine.Application.Dtos;

public class TmdbMovieDetailDto
{
    public int TmdbId { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string TituloOriginal { get; set; } = string.Empty;
    public string Eslogan { get; set; } = "Cine Tradicional en Jilotepec";
    public string Sinopsis { get; set; } = string.Empty;
    public int Duracion { get; set; } = 120;
    public string Clasificacion { get; set; } = "B";
    public string Director { get; set; } = "Director Cinematográfico";
    public string Actores { get; set; } = "Elenco estelar";
    public string Genero { get; set; } = "Cine";
    public string GenerosCompletos { get; set; } = string.Empty;
    public string PosterPath { get; set; } = string.Empty; // Ruta relativa estricta ej: "/qmDpIHrmpJINaRKAfWQfftjCdyi.jpg"
    public string BackdropPath { get; set; } = string.Empty; // Ruta relativa estricta
    public string ReleaseYear { get; set; } = "2026";
    public double Rating { get; set; } = 8.5;
    public string? TrailerKey { get; set; }
}
