using System;
using System.Collections.Generic;

namespace cine_back.Models;

public partial class Rol
{
    public int IdRol { get; set; }

    public string NombreRol { get; set; } = null!;

    public string Descripcion { get; set; } = null!;

    public virtual ICollection<Vendedor> Vendedors { get; set; } = new List<Vendedor>();
}
