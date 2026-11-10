using System;
using System.Collections.Generic;

namespace Cine.Domain.Entities
{
    public partial class EntradaMercancium
    {
        public int IdEntrada { get; set; }

        public int IdProducto { get; set; }

        public string FolioFactura { get; set; } = null!;

        public decimal CostoTotal { get; set; }

        public DateTime? FechaEntrada { get; set; }

        public int? IdFactura { get; set; }

        public int Cantidad { get; set; } = 1;

        public decimal CostoUnitario { get; set; }

        public int CantidadStockAgregada { get; set; } = 1;

        public virtual Producto IdProductoNavigation { get; set; } = null!;

        public virtual FacturaProveedor? IdFacturaNavigation { get; set; }
    }
}
