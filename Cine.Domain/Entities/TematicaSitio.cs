using System;
using System.Collections.Generic;

namespace Cine.Domain.Entities;

public partial class TematicaSitio
{
    public int Id { get; set; }

    public string NombreTematica { get; set; } = null!;

    public string ClaveIdentificadora { get; set; } = null!;

    public bool EsActiva { get; set; }

    public DateTime? FechaActivacion { get; set; }

    public virtual ICollection<PlantillaCss> PlantillasCss { get; set; } = new List<PlantillaCss>();
}
