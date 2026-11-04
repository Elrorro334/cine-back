using System;
using System.Collections.Generic;

namespace Cine.Domain.Entities
{
    public partial class FacturaProveedor
    {
        public int IdFactura { get; set; }

        public int IdProveedor { get; set; }

        public string Folio { get; set; } = null!;

        public decimal TotalPagado { get; set; }

        public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;

        public int? IdVendedor { get; set; }

        public virtual Proveedor IdProveedorNavigation { get; set; } = null!;

        public virtual Vendedor? IdVendedorNavigation { get; set; }

        public virtual ICollection<EntradaMercancium> Entradas { get; set; } = new List<EntradaMercancium>();
    }
}
