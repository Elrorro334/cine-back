using System;
using System.Collections.Generic;

namespace cine_back.Models;

public partial class Pelicula
{
    public int IdPelicula { get; set; }

    public string Titulo { get; set; } = null!;

    public string Sinopsis { get; set; } = null!;

    public int Duracion { get; set; }

    public string? Clasificacion { get; set; }

    public string Director { get; set; } = null!;

    public string PosterImage { get; set; } = null!;

    public virtual ICollection<Funcion> Funcions { get; set; } = new List<Funcion>();
}
