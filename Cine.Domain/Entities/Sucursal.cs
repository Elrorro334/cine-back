using System;
using System.Collections.Generic;

namespace Cine.Domain.Entities;

public partial class Sucursal
{
    public int Id { get; set; }

    public string Nombre { get; set; } = null!;

    public string IpDefecto { get; set; } = null!;

    public virtual ICollection<ConfiguracionSeguridad> ConfiguracionesSeguridad { get; set; } = new List<ConfiguracionSeguridad>();
}
