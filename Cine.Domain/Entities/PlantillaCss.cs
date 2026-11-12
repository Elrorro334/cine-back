using System;

namespace Cine.Domain.Entities;

public partial class PlantillaCss
{
    public int Id { get; set; }

    public int TematicaId { get; set; }

    public string RutaArchivoCss { get; set; } = null!;

    public string PaletaColoresJson { get; set; } = null!;

    public string Tipografia { get; set; } = null!;

    public virtual TematicaSitio TematicaNavigation { get; set; } = null!;
}
