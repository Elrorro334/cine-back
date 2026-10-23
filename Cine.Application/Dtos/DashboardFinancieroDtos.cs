namespace Cine.Application.Dtos;

public class FiltroDashboardDto
{
    public DateTime? FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }
    public int? OffsetSemanas { get; set; }
}

public class DashboardFinancieroDto
{
    public PeriodoDashboardDto Periodo { get; set; } = new();
    public KpiFinancieroDto Kpis { get; set; } = new();
    public List<TransaccionDetalleDto> Transacciones { get; set; } = new();
}

public class PeriodoDashboardDto
{
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    public string DescripcionSemana { get; set; } = string.Empty;
}

public class KpiFinancieroDto
{
    /// <summary>
    /// Tarjeta 1: Total acumulado de ingresos monetarios en el periodo.
    /// </summary>
    public decimal TotalIngresos { get; set; }

    /// <summary>
    /// Tarjeta 2: Total monetario generado por venta de boletos en taquilla.
    /// </summary>
    public decimal TotalTaquilla { get; set; }

    /// <summary>
    /// Cantidad total de boletos de cine vendidos.
    /// </summary>
    public int BoletosVendidos { get; set; }

    /// <summary>
    /// Tarjeta 3: Total monetario generado por dulcería y alimentos.
    /// </summary>
    public decimal TotalDulceria { get; set; }

    /// <summary>
    /// Cantidad de productos de confitería vendidos.
    /// </summary>
    public int ProductosDulceriaVendidos { get; set; }

    /// <summary>
    /// Tarjeta 4: Comparativa de ingresos por método de pago.
    /// </summary>
    public MetodoPagoDesgloseDto Efectivo { get; set; } = new() { Metodo = "Efectivo" };
    public MetodoPagoDesgloseDto Tarjeta { get; set; } = new() { Metodo = "Tarjeta" };
    public MetodoPagoDesgloseDto Otros { get; set; } = new() { Metodo = "Otros" };

    /// <summary>
    /// Total de transacciones del periodo y ticket promedio.
    /// </summary>
    public int TotalTransacciones { get; set; }
    public decimal TicketPromedio { get; set; }
}

public class MetodoPagoDesgloseDto
{
    public string Metodo { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public double Porcentaje { get; set; }
    public int CantidadTransacciones { get; set; }
}

public class TransaccionDetalleDto
{
    public int IdVenta { get; set; }
    public DateTime FechaHora { get; set; }
    public string Cajero { get; set; } = string.Empty;
    public string TipoTransaccion { get; set; } = string.Empty; // "Taquilla", "Dulcería", "Mixto"
    public string MetodoPago { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public int CantidadArticulos { get; set; }
    public string ResumenDetalle { get; set; } = string.Empty;
}

public class SemanaCinematograficaDto
{
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    public string Descripcion { get; set; } = string.Empty;
}
