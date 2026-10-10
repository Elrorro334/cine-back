using System;
using System.Collections.Generic;

namespace cine_back.Models;

public partial class Cliente
{
    public int IdCliente { get; set; }

    public string Nombre { get; set; } = null!;

    public string? NumeroTelefono { get; set; }

    public string Correo { get; set; } = null!;

    public virtual ICollection<Preorden> Preordens { get; set; } = new List<Preorden>();
}
