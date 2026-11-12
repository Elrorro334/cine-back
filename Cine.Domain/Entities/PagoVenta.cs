using System;

namespace Cine.Domain.Entities;

public partial class PagoVenta
{
    public int IdPago { get; set; }

    public int IdVenta { get; set; }

    public string MetodoPago { get; set; } = "Efectivo";

    public decimal MontoTotal { get; set; }

    public decimal MontoRecibido { get; set; }

    public decimal Cambio { get; set; }

    public string? FolioAutorizacion { get; set; }

    public string CodigoBarrasTicket { get; set; } = string.Empty;

    public string RfcEmisor { get; set; } = "CTX-260414-9X0";

    public DateTime FechaPago { get; set; }

    public virtual Ventum IdVentaNavigation { get; set; } = null!;
}
