using System;
using System.Collections.Generic;

namespace cine_back.Models;

public partial class Vendedor
{
    public int IdVendedor { get; set; }

    public string Nombre { get; set; } = null!;

    public int IdRol { get; set; }

    public string PasswordHash { get; set; } = null!;

    public bool EstadoActivo { get; set; }

    public DateTime FechaCreacion { get; set; }

    public virtual Rol IdRolNavigation { get; set; } = null!;

    public virtual ICollection<Ventum> Venta { get; set; } = new List<Ventum>();
}
