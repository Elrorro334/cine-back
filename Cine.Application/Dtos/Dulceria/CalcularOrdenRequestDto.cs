namespace Cine.Application.Dtos;

public class CalcularOrdenRequestDto
{
    public List<RegistrarVentaItemDto> ItemsActuales { get; set; } = new();
    public string? SkuAgregar { get; set; }
    public int? IdProductoAgregar { get; set; }
    public int CantidadAgregar { get; set; } = 1;
}
