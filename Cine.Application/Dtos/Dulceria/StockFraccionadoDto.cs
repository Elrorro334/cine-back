namespace Cine.Application.Dtos;

public class StockFraccionadoDto
{
    public int IdStockFraccionado { get; set; }
    public string NombreInsumo { get; set; } = "PIZZAS";
    public int StockRebanadasActual { get; set; }
    public int FactorFraccion { get; set; } = 8;
    public int PizzasCompletasEquivalentes { get; set; }
    public int RebanadasSueltas { get; set; }
    public string EtiquetaMonitor { get; set; } = string.Empty;
    public ProductoDto? ProductoRebanada { get; set; }
    public ProductoDto? ProductoPizzaCompleta { get; set; }
}
