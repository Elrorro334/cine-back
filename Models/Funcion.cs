using System;
using System.Collections.Generic;

namespace cine_back.Models;

public partial class Funcion
{
    public int IdFuncion { get; set; }

    public int IdPelicula { get; set; }

    public int IdSala { get; set; }

    public DateTime FechaFuncion { get; set; }

    public DateTime HoraInicio { get; set; }

    public decimal Precio { get; set; }

    public virtual Pelicula IdPeliculaNavigation { get; set; } = null!;

    public virtual Sala IdSalaNavigation { get; set; } = null!;

    public virtual ICollection<Preorden> Preordens { get; set; } = new List<Preorden>();
}
