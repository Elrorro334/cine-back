namespace Cine.Application.Dtos;

public class ButacaDto
{
    public int IdButaca { get; set; }
    public string Fila { get; set; } = string.Empty;
    public int Numero { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public bool EstaOcupada { get; set; }
}

public class FuncionButacasDto
{
    public int IdFuncion { get; set; }
    public int IdSala { get; set; }
    public string NombreSala { get; set; } = string.Empty;
    public decimal Precio { get; set; }
    public List<ButacaDto> Butacas { get; set; } = new();
}

public class ProductoDto
{
    public int IdProducto { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Categoria { get; set; } = string.Empty;
    public decimal Precio { get; set; }
    public int Stock { get; set; }
    public string? Img { get; set; }
}

public class RegistrarVentaItemDto
{
    public int? IdProducto { get; set; }
    public int? IdFuncion { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Tipo { get; set; } = "boleto"; // "boleto" o "dulceria"
    public int Cantidad { get; set; }
    public decimal Precio { get; set; }
    public List<string>? Asientos { get; set; }
}

public class RegistrarVentaDto
{
    public int IdVendedor { get; set; }
    public string MetodoPago { get; set; } = "Efectivo";
    public List<RegistrarVentaItemDto> Items { get; set; } = new();
}

public class VentaConfirmadaDto
{
    public int IdVenta { get; set; }
    public string Folio { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public string MetodoPago { get; set; } = string.Empty;
    public DateTime FechaVenta { get; set; }
    public List<RegistrarVentaItemDto> Items { get; set; } = new();
}

public class PreordenComboDto
{
    public string Nombre { get; set; } = string.Empty;
    public int Cantidad { get; set; }
    public decimal Precio { get; set; }
}

public class PreordenDto
{
    public int IdPreorden { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Cliente { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public string Pelicula { get; set; } = string.Empty;
    public string Horario { get; set; } = string.Empty;
    public int IdFuncion { get; set; }
    public int BoletosGeneral { get; set; }
    public decimal PrecioBoleto { get; set; }
    public List<string> Asientos { get; set; } = new();
    public List<PreordenComboDto> Combos { get; set; } = new();
    public decimal Total { get; set; }
    public string FechaCreacion { get; set; } = string.Empty;
    public int VigenciaDias { get; set; }
    public bool EstadoActivo { get; set; }
}
