using System;
using System.Collections.Generic;

namespace cine_back.Models;

public partial class DetallePreordenBoleto
{
    public int IdDetalle { get; set; }

    public int IdPreorden { get; set; }

    public int Caantidad { get; set; }

    public decimal Subtotal { get; set; }

    public virtual Preorden IdPreordenNavigation { get; set; } = null!;
}
