using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cine.Application.Dtos;
using Cine.Application.Helpers;
using Cine.Application.Services;
using Cine.Domain.Entities;
using Cine.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace cine_back.Tests.Services;

public class DashboardFinancieroServiceTests
{
    private static CineDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<CineDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new CineDbContext(options);
    }

    [Fact]
    public async Task ObtenerSemanaActualAsync_ReturnsThursdayToWednesdayRange()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = new DashboardFinancieroService(context);

        // Act
        var result = await service.ObtenerSemanaActualAsync();

        // Assert
        result.Should().NotBeNull();
        result.FechaInicio.DayOfWeek.Should().Be(DayOfWeek.Thursday);
        result.FechaFin.DayOfWeek.Should().Be(DayOfWeek.Wednesday);
        result.Descripcion.Should().Contain("Semana Cinematográfica");
        result.FechaFin.Should().BeAfter(result.FechaInicio);
    }

    [Fact]
    public async Task ObtenerDashboardAsync_WhenNoSalesExist_ReturnsZerosWithoutExceptions()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = new DashboardFinancieroService(context);
        var filtro = new FiltroDashboardDto();

        // Act
        var result = await service.ObtenerDashboardAsync(filtro);

        // Assert
        result.Should().NotBeNull();
        result.Kpis.TotalIngresos.Should().Be(0m);
        result.Kpis.TotalTaquilla.Should().Be(0m);
        result.Kpis.BoletosVendidos.Should().Be(0);
        result.Kpis.TotalDulceria.Should().Be(0m);
        result.Kpis.ProductosDulceriaVendidos.Should().Be(0);
        result.Kpis.Efectivo.Total.Should().Be(0m);
        result.Kpis.Efectivo.Porcentaje.Should().Be(0);
        result.Kpis.Tarjeta.Total.Should().Be(0m);
        result.Kpis.TotalTransacciones.Should().Be(0);
        result.Kpis.TicketPromedio.Should().Be(0m);
        result.Transacciones.Should().BeEmpty();
    }

    [Fact]
    public async Task ObtenerDashboardAsync_WhenSalesExistInCurrentWeek_CalculatesKpisAccurately()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var (inicioSemana, finSemana, _) = SemanaCinematograficaHelper.CalcularSemana(DateTime.Today, 0);

        var rolAdmin = new Rol { IdRol = 1, NombreRol = "Administrador", Descripcion = "Admin" };
        var vendedor = new Vendedor
        {
            IdVendedor = 101,
            Nombre = "Juan Cajero",
            IdRol = 1,
            PasswordHash = "hash",
            EstadoActivo = true,
            FechaCreacion = DateTime.Today
        };

        var catDulceria = new Categorium { IdCategoria = 1, Nombre = "Dulcería", Descripcion = "Snacks" };
        var catBoletos = new Categorium { IdCategoria = 2, Nombre = "Boletos", Descripcion = "Entradas de cine" };

        var prodPalomitas = new Producto
        {
            IdProducto = 201,
            Nombre = "Palomitas Grandes",
            CodigoBarras = "PAL01",
            IdCategoria = 1,
            IdCategoriaNavigation = catDulceria,
            PrecioVenta = 85.00m,
            Costo = 20.00m
        };

        var prodBoleto = new Producto
        {
            IdProducto = 202,
            Nombre = "Boleto Tradicional",
            CodigoBarras = "BOL01",
            IdCategoria = 2,
            IdCategoriaNavigation = catBoletos,
            PrecioVenta = 75.00m,
            Costo = 0m
        };

        // Venta 1: Efectivo - Mixta (1 boleto + 1 palomitas)
        var venta1 = new Ventum
        {
            IdVenta = 1001,
            IdVendedor = 101,
            IdVendedorNavigation = vendedor,
            FechaVenta = inicioSemana.AddHours(14), // Dentro de la semana cinematográfica
            Total = 160.00m,
            MetodoPago = "Efectivo",
            DetalleVenta = new List<DetalleVentum>
            {
                new DetalleVentum { IdDetalleVenta = 1, IdProducto = 202, IdProductoNavigation = prodBoleto, Cantidad = 1 },
                new DetalleVentum { IdDetalleVenta = 2, IdProducto = 201, IdProductoNavigation = prodPalomitas, Cantidad = 1 }
            }
        };

        // Venta 2: Tarjeta - Solo Boletos (2 boletos)
        var venta2 = new Ventum
        {
            IdVenta = 1002,
            IdVendedor = 101,
            IdVendedorNavigation = vendedor,
            FechaVenta = inicioSemana.AddDays(1).AddHours(18),
            Total = 150.00m,
            MetodoPago = "Tarjeta",
            DetalleVenta = new List<DetalleVentum>
            {
                new DetalleVentum { IdDetalleVenta = 3, IdProducto = 202, IdProductoNavigation = prodBoleto, Cantidad = 2 }
            }
        };

        // Venta 3: Fuera de la semana (semana anterior)
        var ventaFuera = new Ventum
        {
            IdVenta = 9999,
            IdVendedor = 101,
            IdVendedorNavigation = vendedor,
            FechaVenta = inicioSemana.AddDays(-10),
            Total = 500.00m,
            MetodoPago = "Efectivo"
        };

        context.Rols.Add(rolAdmin);
        context.Vendedors.Add(vendedor);
        context.Categoria.AddRange(catDulceria, catBoletos);
        context.Productos.AddRange(prodPalomitas, prodBoleto);
        context.Venta.AddRange(venta1, venta2, ventaFuera);
        await context.SaveChangesAsync();

        var service = new DashboardFinancieroService(context);
        var filtro = new FiltroDashboardDto(); // Default a semana actual

        // Act
        var dashboard = await service.ObtenerDashboardAsync(filtro);

        // Assert
        dashboard.Should().NotBeNull();
        dashboard.Kpis.TotalIngresos.Should().Be(310.00m); // 160 + 150 (ignora la venta fuera de fecha)
        dashboard.Kpis.TotalTaquilla.Should().Be(225.00m); // 75 + 150
        dashboard.Kpis.BoletosVendidos.Should().Be(3);     // 1 + 2
        dashboard.Kpis.TotalDulceria.Should().Be(85.00m);   // 85
        dashboard.Kpis.ProductosDulceriaVendidos.Should().Be(1);

        // Comparativa Métodos de Pago
        dashboard.Kpis.Efectivo.Total.Should().Be(160.00m);
        dashboard.Kpis.Efectivo.CantidadTransacciones.Should().Be(1);
        dashboard.Kpis.Tarjeta.Total.Should().Be(150.00m);
        dashboard.Kpis.Tarjeta.CantidadTransacciones.Should().Be(1);

        // Transacciones
        dashboard.Transacciones.Should().HaveCount(2);
        dashboard.Transacciones.First().TipoTransaccion.Should().Be("Taquilla");
        dashboard.Transacciones.Last().TipoTransaccion.Should().Be("Mixto");
        dashboard.Kpis.TicketPromedio.Should().Be(155.00m);
    }

    [Fact]
    public async Task ObtenerDashboardAsync_WhenCustomDateRangeProvided_FiltersExactlyByDates()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var vendedor = new Vendedor { IdVendedor = 1, Nombre = "Cajero Test", IdRol = 1, PasswordHash = "h", EstadoActivo = true, FechaCreacion = DateTime.Today };

        var fechaFiltroInicio = new DateTime(2026, 9, 1);
        var fechaFiltroFin = new DateTime(2026, 9, 5);

        var ventaEnRango = new Ventum
        {
            IdVenta = 501,
            IdVendedor = 1,
            IdVendedorNavigation = vendedor,
            FechaVenta = new DateTime(2026, 9, 3, 15, 30, 0),
            Total = 200m,
            MetodoPago = "Tarjeta"
        };

        var ventaFueraRango = new Ventum
        {
            IdVenta = 502,
            IdVendedor = 1,
            IdVendedorNavigation = vendedor,
            FechaVenta = new DateTime(2026, 9, 10, 12, 0, 0),
            Total = 300m,
            MetodoPago = "Efectivo"
        };

        context.Vendedors.Add(vendedor);
        context.Venta.AddRange(ventaEnRango, ventaFueraRango);
        await context.SaveChangesAsync();

        var service = new DashboardFinancieroService(context);
        var filtro = new FiltroDashboardDto
        {
            FechaInicio = fechaFiltroInicio,
            FechaFin = fechaFiltroFin
        };

        // Act
        var dashboard = await service.ObtenerDashboardAsync(filtro);

        // Assert
        dashboard.Should().NotBeNull();
        dashboard.Kpis.TotalIngresos.Should().Be(200m);
        dashboard.Transacciones.Should().HaveCount(1);
        dashboard.Transacciones.First().IdVenta.Should().Be(501);
        dashboard.Periodo.DescripcionSemana.Should().Contain("01/09/2026");
    }

    [Fact]
    public async Task ExportarTransaccionesCsvAsync_GeneratesStructuredCsvWithBomAndHeaders()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var vendedor = new Vendedor { IdVendedor = 1, Nombre = "Cajera Ana", IdRol = 1, PasswordHash = "h", EstadoActivo = true, FechaCreacion = DateTime.Today };

        var venta = new Ventum
        {
            IdVenta = 701,
            IdVendedor = 1,
            IdVendedorNavigation = vendedor,
            FechaVenta = DateTime.Today.AddHours(10),
            Total = 120.50m,
            MetodoPago = "Efectivo"
        };

        context.Vendedors.Add(vendedor);
        context.Venta.Add(venta);
        await context.SaveChangesAsync();

        var service = new DashboardFinancieroService(context);

        // Act
        byte[] bytes = await service.ExportarTransaccionesCsvAsync(new FiltroDashboardDto());

        // Assert
        bytes.Should().NotBeNull();
        bytes.Length.Should().BeGreaterThan(0);

        // Verificar el BOM de UTF-8 (0xEF, 0xBB, 0xBF)
        bytes[0].Should().Be(0xEF);
        bytes[1].Should().Be(0xBB);
        bytes[2].Should().Be(0xBF);

        string csvContent = Encoding.UTF8.GetString(bytes);
        csvContent.Should().Contain("Folio,Fecha,Hora,Cajero,Tipo Transaccion,Metodo Pago,Total");
        csvContent.Should().Contain("701");
        csvContent.Should().Contain("Cajera Ana");
        csvContent.Should().Contain("120.50");
        csvContent.Should().Contain("RESUMEN DE INGRESOS");
    }
}
