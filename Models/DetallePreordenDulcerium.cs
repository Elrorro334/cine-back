using System;
using System.Collections.Generic;

namespace cine_back.Models;

public partial class DetallePreordenDulcerium
{
    public int IdDetalle { get; set; }

    public int IdPreorden { get; set; }

    public int IdProducto { get; set; }

    public int Cantidad { get; set; }

    public virtual Preorden IdPreordenNavigation { get; set; } = null!;

    public virtual Producto IdProductoNavigation { get; set; } = null!;
}
