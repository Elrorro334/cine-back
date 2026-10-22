using System.Globalization;
using System.Text;
using Cine.Application.Dtos;
using Cine.Application.Helpers;
using Cine.Application.Interfaces;
using Cine.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Cine.Application.Services;

public class DashboardFinancieroService : IDashboardFinancieroService
{
    private const string MetodoEfectivo = "Efectivo";
    private const string MetodoTarjeta = "Tarjeta";
    private const string MetodoOtros = "Otros";

    private readonly ICineDbContext _db;

    public DashboardFinancieroService(ICineDbContext db)
    {
        _db = db;
    }

    public async Task<DashboardFinancieroDto> ObtenerDashboardAsync(FiltroDashboardDto filtro)
    {
        var (inicio, fin, descripcion) = ResolverRangoFechas(filtro);

        // 1. Consultar ventas del periodo con vendedor y detalles de producto
        var ventas = await _db.Venta
            .Include(v => v.IdVendedorNavigation)
            .Include(v => v.DetalleVenta)
                .ThenInclude(d => d.IdProductoNavigation.IdCategoriaNavigation)
            .Where(v => v.FechaVenta >= inicio && v.FechaVenta <= fin)
            .OrderByDescending(v => v.FechaVenta)
            .ToListAsync();

        // 2. Consultar preórdenes del periodo
        var preordenes = await _db.Preordens
            .Include(p => p.DetallePreordenBoletos)
            .Where(p => p.FechaCreacion >= inicio && p.FechaCreacion <= fin && p.EstadoActivo)
            .ToListAsync();

        // 3. Calcular totales y métricas de KPIs
        decimal totalIngresosVentas = ventas.Sum(v => v.Total);
        var (totalTaquilla, boletosVendidos, totalDulceria, productosDulceriaVendidos, listaTransacciones) =
            ProcesarColeccionVentas(ventas);

        // Incorporar boletos de preórdenes si no estuvieran registrados en Venta
        if (preordenes.Count > 0 && ventas.Count == 0)
        {
            var subtotalPreordenBoletos = preordenes.SelectMany(p => p.DetallePreordenBoletos).Sum(b => b.Subtotal);
            var cantidadPreordenBoletos = preordenes.SelectMany(p => p.DetallePreordenBoletos).Sum(b => b.Caantidad);
            totalTaquilla += subtotalPreordenBoletos;
            boletosVendidos += cantidadPreordenBoletos;
            totalIngresosVentas += subtotalPreordenBoletos;
        }

        // Si los subtotales de productos no cubren el total de ingresos, cuadrar con taquilla
        if (totalIngresosVentas > (totalTaquilla + totalDulceria))
        {
            totalTaquilla += (totalIngresosVentas - (totalTaquilla + totalDulceria));
        }

        // 4. Calcular métricas de métodos de pago
        var desglosePago = CalcularDesgloseMetodosPago(ventas, totalIngresosVentas);

        return new DashboardFinancieroDto
        {
            Periodo = new PeriodoDashboardDto
            {
                FechaInicio = inicio,
                FechaFin = fin,
                DescripcionSemana = descripcion
            },
            Kpis = new KpiFinancieroDto
            {
                TotalIngresos = totalIngresosVentas,
                TotalTaquilla = totalTaquilla,
                BoletosVendidos = boletosVendidos,
                TotalDulceria = totalDulceria,
                ProductosDulceriaVendidos = productosDulceriaVendidos,
                Efectivo = desglosePago.Efectivo,
                Tarjeta = desglosePago.Tarjeta,
                Otros = desglosePago.Otros,
                TotalTransacciones = listaTransacciones.Count,
                TicketPromedio = listaTransacciones.Count > 0
                    ? Math.Round(totalIngresosVentas / listaTransacciones.Count, 2)
                    : 0m
            },
            Transacciones = listaTransacciones
        };
    }

    public Task<SemanaCinematograficaDto> ObtenerSemanaActualAsync()
    {
        var (inicio, fin, descripcion) = SemanaCinematograficaHelper.CalcularSemana(DateTime.Today, 0);

        return Task.FromResult(new SemanaCinematograficaDto
        {
            FechaInicio = inicio,
            FechaFin = fin,
            Descripcion = descripcion
        });
    }

    public async Task<byte[]> ExportarTransaccionesCsvAsync(FiltroDashboardDto filtro)
    {
        var dashboard = await ObtenerDashboardAsync(filtro);

        var sb = new StringBuilder();
        sb.AppendLine("Folio,Fecha,Hora,Cajero,Tipo Transaccion,Metodo Pago,Total,Cantidad Articulos,Detalle de Productos");

        foreach (var t in dashboard.Transacciones)
        {
            string fecha = t.FechaHora.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
            string hora = t.FechaHora.ToString("HH:mm:ss", CultureInfo.InvariantCulture);
            string cajeroEscapado = EscaparCsv(t.Cajero);
            string tipoEscapado = EscaparCsv(t.TipoTransaccion);
            string metodoEscapado = EscaparCsv(t.MetodoPago);
            string total = t.Total.ToString("F2", CultureInfo.InvariantCulture);
            string detalleEscapado = EscaparCsv(t.ResumenDetalle);

            sb.AppendLine($"{t.IdVenta},{fecha},{hora},{cajeroEscapado},{tipoEscapado},{metodoEscapado},{total},{t.CantidadArticulos},{detalleEscapado}");
        }

        sb.AppendLine();
        sb.AppendLine($"RESUMEN DE INGRESOS,,,,,,{dashboard.Kpis.TotalIngresos.ToString("F2", CultureInfo.InvariantCulture)},{dashboard.Kpis.BoletosVendidos + dashboard.Kpis.ProductosDulceriaVendidos},Total Transacciones: {dashboard.Kpis.TotalTransacciones}");
        sb.AppendLine($"TOTAL TAQUILLA,,,,,,{dashboard.Kpis.TotalTaquilla.ToString("F2", CultureInfo.InvariantCulture)},{dashboard.Kpis.BoletosVendidos},Boletos Vendidos");
        sb.AppendLine($"TOTAL DULCERIA,,,,,,{dashboard.Kpis.TotalDulceria.ToString("F2", CultureInfo.InvariantCulture)},{dashboard.Kpis.ProductosDulceriaVendidos},Articulos Dulceria");

        var preamble = Encoding.UTF8.GetPreamble();
        var contentBytes = Encoding.UTF8.GetBytes(sb.ToString());

        var resultBytes = new byte[preamble.Length + contentBytes.Length];
        Buffer.BlockCopy(preamble, 0, resultBytes, 0, preamble.Length);
        Buffer.BlockCopy(contentBytes, 0, resultBytes, preamble.Length, contentBytes.Length);

        return resultBytes;
    }

    private static (decimal TotalTaquilla, int BoletosVendidos, decimal TotalDulceria, int ProductosDulceriaVendidos, List<TransaccionDetalleDto> Transacciones)
        ProcesarColeccionVentas(List<Ventum> ventas)
    {
        decimal totalTaquilla = 0m;
        int boletosVendidos = 0;
        decimal totalDulceria = 0m;
        int productosDulceriaVendidos = 0;
        var listaTransacciones = new List<TransaccionDetalleDto>();

        foreach (var venta in ventas)
        {
            var (boletosEnVenta, subtotalBoletos, dulceriaEnVenta, subtotalDulceria, transaccion) =
                ProcesarVentaIndividual(venta);

            totalTaquilla += subtotalBoletos;
            boletosVendidos += boletosEnVenta;
            totalDulceria += subtotalDulceria;
            productosDulceriaVendidos += dulceriaEnVenta;
            listaTransacciones.Add(transaccion);
        }

        return (totalTaquilla, boletosVendidos, totalDulceria, productosDulceriaVendidos, listaTransacciones);
    }

    private static (int Boletos, decimal SubtotalBoletos, int Dulceria, decimal SubtotalDulceria, TransaccionDetalleDto Transaccion)
        ProcesarVentaIndividual(Ventum venta)
    {
        int boletosEnVenta = 0;
        decimal subtotalBoletosEnVenta = 0m;
        int dulceriaEnVenta = 0;
        decimal subtotalDulceriaEnVenta = 0m;
        var resumenItems = new List<string>();

        if (venta.DetalleVenta != null)
        {
            foreach (var detalle in venta.DetalleVenta)
            {
                var prod = detalle.IdProductoNavigation;
                var catNombre = prod?.IdCategoriaNavigation?.Nombre ?? string.Empty;
                var prodNombre = prod?.Nombre ?? "Artículo";
                decimal precio = prod?.PrecioVenta ?? 0m;
                decimal subtotal = precio * detalle.Cantidad;

                if (EsBoleto(catNombre, prodNombre))
                {
                    boletosEnVenta += detalle.Cantidad;
                    subtotalBoletosEnVenta += subtotal;
                }
                else
                {
                    dulceriaEnVenta += detalle.Cantidad;
                    subtotalDulceriaEnVenta += subtotal;
                }

                resumenItems.Add($"{detalle.Cantidad}x {prodNombre}");
            }
        }

        if (boletosEnVenta == 0 && dulceriaEnVenta == 0)
        {
            subtotalBoletosEnVenta = venta.Total;
            boletosEnVenta = venta.Total > 0 ? 1 : 0;
            resumenItems.Add("Venta Directa de Taquilla");
        }

        var transaccion = new TransaccionDetalleDto
        {
            IdVenta = venta.IdVenta,
            FechaHora = venta.FechaVenta,
            Cajero = venta.IdVendedorNavigation?.Nombre ?? "Cajero General",
            TipoTransaccion = DeterminarTipoTransaccion(boletosEnVenta, dulceriaEnVenta),
            MetodoPago = NormalizarMetodoPago(venta.MetodoPago),
            Total = venta.Total,
            CantidadArticulos = boletosEnVenta + dulceriaEnVenta,
            ResumenDetalle = string.Join(", ", resumenItems)
        };

        return (boletosEnVenta, subtotalBoletosEnVenta, dulceriaEnVenta, subtotalDulceriaEnVenta, transaccion);
    }

    private static (DateTime Inicio, DateTime Fin, string Descripcion) ResolverRangoFechas(FiltroDashboardDto filtro)
    {
        if (filtro.FechaInicio.HasValue && filtro.FechaFin.HasValue)
        {
            var inicio = filtro.FechaInicio.Value.Date;
            var fin = filtro.FechaFin.Value.Date.AddHours(23).AddMinutes(59).AddSeconds(59);
            string descripcion = $"Periodo personalizado del {inicio:dd/MM/yyyy} al {fin:dd/MM/yyyy}";
            return (inicio, fin, descripcion);
        }

        return SemanaCinematograficaHelper.CalcularSemana(DateTime.Today, filtro.OffsetSemanas ?? 0);
    }

    private static bool EsBoleto(string categoria, string producto)
    {
        return categoria.Contains("boleto", StringComparison.OrdinalIgnoreCase) ||
               categoria.Contains("taquilla", StringComparison.OrdinalIgnoreCase) ||
               producto.Contains("boleto", StringComparison.OrdinalIgnoreCase) ||
               producto.Contains("entrada", StringComparison.OrdinalIgnoreCase);
    }

    private static string DeterminarTipoTransaccion(int boletos, int dulceria)
    {
        if (boletos > 0 && dulceria > 0) return "Mixto";
        if (dulceria > 0) return "Dulcería";
        return "Taquilla";
    }

    private static string NormalizarMetodoPago(string? metodo)
    {
        if (string.IsNullOrWhiteSpace(metodo)) return MetodoEfectivo;

        if (metodo.Contains("tarjeta", StringComparison.OrdinalIgnoreCase) ||
            metodo.Contains("card", StringComparison.OrdinalIgnoreCase) ||
            metodo.Contains("debito", StringComparison.OrdinalIgnoreCase) ||
            metodo.Contains("credito", StringComparison.OrdinalIgnoreCase))
        {
            return MetodoTarjeta;
        }

        if (metodo.Contains("efectivo", StringComparison.OrdinalIgnoreCase) ||
            metodo.Contains("cash", StringComparison.OrdinalIgnoreCase))
        {
            return MetodoEfectivo;
        }

        return metodo.Trim();
    }

    private static (MetodoPagoDesgloseDto Efectivo, MetodoPagoDesgloseDto Tarjeta, MetodoPagoDesgloseDto Otros) CalcularDesgloseMetodosPago(
        List<Ventum> ventas, decimal totalIngresos)
    {
        decimal totalEfectivo = 0m;
        int countEfectivo = 0;

        decimal totalTarjeta = 0m;
        int countTarjeta = 0;

        decimal totalOtros = 0m;
        int countOtros = 0;

        foreach (var v in ventas)
        {
            string norm = NormalizarMetodoPago(v.MetodoPago);
            if (norm == MetodoEfectivo)
            {
                totalEfectivo += v.Total;
                countEfectivo++;
            }
            else if (norm == MetodoTarjeta)
            {
                totalTarjeta += v.Total;
                countTarjeta++;
            }
            else
            {
                totalOtros += v.Total;
                countOtros++;
            }
        }

        double pctEfectivo = totalIngresos > 0 ? Math.Round((double)(totalEfectivo / totalIngresos) * 100, 2) : 0;
        double pctTarjeta = totalIngresos > 0 ? Math.Round((double)(totalTarjeta / totalIngresos) * 100, 2) : 0;
        double pctOtros = totalIngresos > 0 ? Math.Round((double)(totalOtros / totalIngresos) * 100, 2) : 0;

        return (
            new MetodoPagoDesgloseDto
            {
                Metodo = MetodoEfectivo,
                Total = totalEfectivo,
                Porcentaje = pctEfectivo,
                CantidadTransacciones = countEfectivo
            },
            new MetodoPagoDesgloseDto
            {
                Metodo = MetodoTarjeta,
                Total = totalTarjeta,
                Porcentaje = pctTarjeta,
                CantidadTransacciones = countTarjeta
            },
            new MetodoPagoDesgloseDto
            {
                Metodo = MetodoOtros,
                Total = totalOtros,
                Porcentaje = pctOtros,
                CantidadTransacciones = countOtros
            }
        );
    }

    private static string EscaparCsv(string valor)
    {
        if (string.IsNullOrEmpty(valor)) return "\"\"";
        if (valor.Contains(',') || valor.Contains('"') || valor.Contains('\n') || valor.Contains('\r'))
        {
            return $"\"{valor.Replace("\"", "\"\"")}\"";
        }
        return valor;
    }
}
