namespace Cine.Application.Dtos;

public class ResumenOrdenCalculadoDto
{
    public List<OrdenLineaCalculadaDto> Items { get; set; } = new();
    public int TotalArticulos { get; set; }
    public decimal SubtotalBoletos { get; set; }
    public decimal SubtotalDulceria { get; set; }
    public decimal TotalVenta { get; set; }
    public StockFraccionadoDto? StockFraccionado { get; set; }
    public List<ProductoDto> ProductosActualizados { get; set; } = new();
    public ProductoDto? ProductoAgregado { get; set; }
}
