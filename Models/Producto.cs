using System;
using System.Collections.Generic;

namespace cine_back.Models;

public partial class Producto
{
    public int IdProducto { get; set; }

    public string CodigoBarras { get; set; } = null!;

    public string Nombre { get; set; } = null!;

    public int IdCategoria { get; set; }

    public decimal PrecioVenta { get; set; }

    public decimal Costo { get; set; }

    public int StockActual { get; set; }

    public int StockMinimo { get; set; }

    public virtual ICollection<DetallePreordenDulcerium> DetallePreordenDulceria { get; set; } = new List<DetallePreordenDulcerium>();

    public virtual ICollection<DetalleVentum> DetalleVenta { get; set; } = new List<DetalleVentum>();

    public virtual ICollection<EntradaMercancium> EntradaMercancia { get; set; } = new List<EntradaMercancium>();

    public virtual Categorium IdCategoriaNavigation { get; set; } = null!;
}
