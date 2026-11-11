using System;

namespace Cine.Domain.Entities;

public partial class CanjePreorden
{
    public int IdCanje { get; set; }

    public int IdPreorden { get; set; }

    public int IdVenta { get; set; }

    public int IdVendedor { get; set; }

    public string CodigoQr { get; set; } = string.Empty;

    public DateTime FechaCanje { get; set; }

    public decimal MontoCanjeado { get; set; }

    public virtual Preorden IdPreordenNavigation { get; set; } = null!;

    public virtual Ventum IdVentaNavigation { get; set; } = null!;

    public virtual Vendedor IdVendedorNavigation { get; set; } = null!;
}
