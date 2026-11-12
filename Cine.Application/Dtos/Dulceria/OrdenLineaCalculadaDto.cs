namespace Cine.Application.Dtos;

public class OrdenLineaCalculadaDto
{
    public string IdLinea { get; set; } = string.Empty;
    public int? IdProducto { get; set; }
    public int? IdFuncion { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Tipo { get; set; } = "dulceria";
    public string EtiquetaCategoria { get; set; } = "DULCERÍA";
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Subtotal { get; set; }
    public List<string>? Asientos { get; set; }
    public string? NombreSala { get; set; }
    public bool EsPreorden { get; set; }
    public string? CodigoPreorden { get; set; }
}
