using System;
using System.Collections.Generic;

namespace cine_back.Models;

public partial class EntradaMercancium
{
    public int IdEntrada { get; set; }

    public int IdProducto { get; set; }

    public string FolioFactura { get; set; } = null!;

    public decimal CostoTotal { get; set; }

    public DateTime? FechaEntrada { get; set; }

    public virtual Producto IdProductoNavigation { get; set; } = null!;
}
