namespace Cine.Application.Dtos;

public class CatalogoDulceriaPosDto
{
    public StockFraccionadoDto? StockFraccionado { get; set; }
    public List<ProductoDto> SnacksYBebidas { get; set; } = new();
    public List<ProductoDto> TodosLosProductos { get; set; } = new();
}
