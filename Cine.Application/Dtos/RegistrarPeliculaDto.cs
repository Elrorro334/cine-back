namespace Cine.Application.Dtos;

public class RegistrarPeliculaDto
{
    public string Titulo { get; set; } = string.Empty;
    public string Sinopsis { get; set; } = string.Empty;
    public int Duracion { get; set; } = 120;
    public string Clasificacion { get; set; } = "B";
    public string Director { get; set; } = string.Empty;
    public string PosterPath { get; set; } = string.Empty; // Ruta relativa estricta ej: "/qmDpIHrmpJINaRKAfWQfftjCdyi.jpg"
    public decimal Precio { get; set; } = 50.00m;
    public string? Horario { get; set; } = "16:00";
    public int? IdSala { get; set; } = 1;
}
