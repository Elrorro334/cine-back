using System;

namespace Cine.Domain.Entities
{
    public partial class InventarioStockFraccionado
    {
        public int IdStockFraccionado { get; set; }

        public int IdProductoMaestro { get; set; }

        public int IdProductoFraccion { get; set; }

        public int FactorFraccion { get; set; } = 8;

        public int StockFraccionesActual { get; set; }

        public int StockFraccionesMinimo { get; set; }

        public string UnidadMedidaFraccion { get; set; } = "rebanadas";

        public string UnidadMedidaMaestra { get; set; } = "pizzas";

        public DateTime UltimaActualizacion { get; set; }

        public virtual Producto IdProductoMaestroNavigation { get; set; } = null!;

        public virtual Producto IdProductoFraccionNavigation { get; set; } = null!;
    }
}
