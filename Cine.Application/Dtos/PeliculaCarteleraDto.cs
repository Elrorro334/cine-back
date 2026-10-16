namespace Cine.Application.Dtos
{
    public class PeliculaCarteleraDto
    {
        public int IdPelicula { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string Sinopsis { get; set; } = string.Empty;
        public int Duracion { get; set; } = 120;
        public string Clasificacion { get; set; } = string.Empty;
        public string Director { get; set; } = string.Empty;

        // Ruta relativa del recurso en TMDB (ej: "/mo7teil1qH0SxgLijnqeYP1Eb4w.jpg")
        public string poster_image { get; set; } = string.Empty;
        public string poster_path => poster_image;

        public List<FuncionHorarioDto> Funciones { get; set; } = new List<FuncionHorarioDto>();
    }
}