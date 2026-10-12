using System;
using System.Collections.Generic;

namespace cine_back.Models;

public partial class Sala
{
    public int IdSala { get; set; }

    public string NombreSala { get; set; } = null!;

    public int CapacidadTotal { get; set; }

    public string TipoPantalla { get; set; } = null!;

    public virtual ICollection<Butaca> Butacas { get; set; } = new List<Butaca>();

    public virtual ICollection<Funcion> Funcions { get; set; } = new List<Funcion>();
}
