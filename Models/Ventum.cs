using System;
using System.Collections.Generic;

namespace cine_back.Models;

public partial class Ventum
{
    public int IdVenta { get; set; }

    public int IdVendedor { get; set; }

    public DateTime FechaVenta { get; set; }

    public decimal Total { get; set; }

    public string? MetodoPago { get; set; }

    public virtual ICollection<DetalleVentum> DetalleVenta { get; set; } = new List<DetalleVentum>();

    public virtual Vendedor IdVendedorNavigation { get; set; } = null!;
}
