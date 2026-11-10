using System.Collections.Generic;

namespace Cine.Application.Dtos;

public class CrearPreordenItemDulceriaDto
{
    public int IdProducto { get; set; }
    public int Cantidad { get; set; }
}

public class CrearPreordenDto
{
    public int IdFuncion { get; set; }
    public int Boletos { get; set; }
    public string NombreCliente { get; set; } = string.Empty;
    public string TelefonoCliente { get; set; } = string.Empty;
    public List<CrearPreordenItemDulceriaDto>? Combos { get; set; }
}
