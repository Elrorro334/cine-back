using Cine.Application.Dtos.Funcion;

namespace Cine.Application.Dtos.Peliculas
{
    public class PeliculaCarteleraDto
    {
        public int IdPelicula { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string Clasificacion { get; set; } = string.Empty;
        public string Director { get; set; } = string.Empty;

        // Útil si guardan las portadas en el servidor o usan URLs externas
        public string poster_image { get; set; } = string.Empty;


        public List<FuncionHorarioDto> Funciones { get; set; } = new List<FuncionHorarioDto>();
    }
}