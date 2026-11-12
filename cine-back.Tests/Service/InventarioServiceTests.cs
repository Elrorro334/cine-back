using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cine.Application.Dtos;
using Cine.Application.Services;
using Cine.Domain.Entities;
using Cine.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace cine_back.Tests.Services;

public class InventarioServiceTests
{
    private static CineDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<CineDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new CineDbContext(options);
        SeedBaseData(context);
        return context;
    }

    private static void SeedBaseData(CineDbContext context)
    {
        var catTaquilla = new Categorium { IdCategoria = 1, Nombre = "Taquilla" };
        var catPalomitas = new Categorium { IdCategoria = 2, Nombre = "Palomitas" };
        var catBebidas = new Categorium { IdCategoria = 3, Nombre = "Bebidas" };
        var catAlimentos = new Categorium { IdCategoria = 4, Nombre = "Alimentos" };
        var catDulces = new Categorium { IdCategoria = 5, Nombre = "Dulces" };
        var catCombos = new Categorium { IdCategoria = 6, Nombre = "Combos" };

        context.Categoria.AddRange(catTaquilla, catPalomitas, catBebidas, catAlimentos, catDulces, catCombos);

        var pBoleto = new Producto
        {
            IdProducto = 1,
            CodigoBarras = "BOL-GEN",
            Nombre = "Boleto General",
            IdCategoria = 1,
            PrecioVenta = 50m,
            Costo = 0m,
            StockActual = 99999,
            StockMinimo = 10,
            FactorEquivalencia = 1
        };

        var pPalomitas = new Producto
        {
            IdProducto = 2,
            CodigoBarras = "7501001",
            Nombre = "Palomitas Clásicas",
            IdCategoria = 2,
            PrecioVenta = 65m,
            Costo = 15m,
            StockActual = 150,
            StockMinimo = 20,
            FactorEquivalencia = 1
        };

        var pNachos = new Producto
        {
            IdProducto = 3,
            CodigoBarras = "7501003",
            Nombre = "Nachos con Queso",
            IdCategoria = 4, // Alimentos -> Comida
            PrecioVenta = 55m,
            Costo = 18m,
            StockActual = 15, // Exactamente igual al mínimo de alerta
            StockMinimo = 15,
            FactorEquivalencia = 1
        };

        var pPizza = new Producto
        {
            IdProducto = 4,
            CodigoBarras = "7501006",
            Nombre = "Pizza Personal Pepperoni",
            IdCategoria = 4, // Alimentos -> Comida
            PrecioVenta = 75m,
            Costo = 25m,
            StockActual = 40,
            StockMinimo = 10,
            FactorEquivalencia = 8 // Stock fraccionado (1 pizza = 8 rebanadas)
        };

        var pMms = new Producto
        {
            IdProducto = 5,
            CodigoBarras = "7501005",
            Nombre = "M&M's Chocolate",
            IdCategoria = 5, // Dulces
            PrecioVenta = 35m,
            Costo = 12m,
            StockActual = 5, // Menor al mínimo -> Alerta
            StockMinimo = 10,
            FactorEquivalencia = 1
        };

        var pCombo = new Producto
        {
            IdProducto = 6,
            CodigoBarras = "COMBO01",
            Nombre = "Combo Pareja",
            IdCategoria = 6, // Combos -> Excluido
            PrecioVenta = 130m,
            Costo = 32m,
            StockActual = 80,
            StockMinimo = 10,
            FactorEquivalencia = 1
        };

        context.Productos.AddRange(pBoleto, pPalomitas, pNachos, pPizza, pMms, pCombo);

        var prov1 = new Proveedor
        {
            IdProveedor = 1,
            Nombre = "Distribuidora Central de Dulces",
            Rfc = "DCD850315AB1",
            Telefono = "222-345-6789",
            Contacto = "Lic. Roberto Morales",
            Activo = true
        };

        var prov2 = new Proveedor
        {
            IdProveedor = 2,
            Nombre = "Bebidas y Refrescos de Oriente",
            Rfc = "BRO920810XY2",
            Telefono = "246-123-4567",
            Contacto = "Ing. Patricia Ramos",
            Activo = true
        };

        context.Proveedores.AddRange(prov1, prov2);
        context.SaveChanges();
    }

    [Fact]
    public async Task ObtenerInventarioMaestro_ExcludesTaquillaAndCombos_AndMapsCategories()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = new InventarioService(context);

        // Act
        var result = await service.ObtenerInventarioMaestroAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(4); // Palomitas, Nachos, Pizza, M&M's (Taquilla y Combo excluidos)
        result.Select(p => p.Sku).Should().NotContain("BOL-GEN");
        result.Select(p => p.Sku).Should().NotContain("COMBO01");

        // Verificación de mapeo
        var palomitas = result.First(p => p.Sku == "7501001");
        palomitas.Categoria.Should().Be("Snacks");

        var nachos = result.First(p => p.Sku == "7501003");
        nachos.Categoria.Should().Be("Comida");

        var dulces = result.First(p => p.Sku == "7501005");
        dulces.Categoria.Should().Be("Dulces");
    }

    [Fact]
    public async Task ObtenerInventarioMaestro_Criterio1_DesabastoInminente_WhenStockLessOrEqualToMinimum_SetsEnAlertaTrue()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = new InventarioService(context);

        // Act
        var result = await service.ObtenerInventarioMaestroAsync();

        // Assert
        // Nachos: StockActual = 15, StockMinimo = 15 -> EnAlerta = true
        var nachos = result.First(p => p.Sku == "7501003");
        nachos.EnAlerta.Should().BeTrue();

        // M&M's: StockActual = 5, StockMinimo = 10 -> EnAlerta = true
        var mms = result.First(p => p.Sku == "7501005");
        mms.EnAlerta.Should().BeTrue();

        // Palomitas: StockActual = 150, StockMinimo = 20 -> EnAlerta = false
        var palomitas = result.First(p => p.Sku == "7501001");
        palomitas.EnAlerta.Should().BeFalse();
    }

    [Fact]
    public async Task ObtenerInventarioMaestro_CalculatesMargenPorcentajeCorrectly()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = new InventarioService(context);

        // Act
        var result = await service.ObtenerInventarioMaestroAsync();

        // Assert
        // Palomitas: Precio = 65, Costo = 15 -> Margen = (65 - 15) / 65 * 100 = 76.92%
        var palomitas = result.First(p => p.Sku == "7501001");
        palomitas.MargenPorcentaje.Should().Be(76.92m);
    }

    [Fact]
    public async Task ActualizarPrecios_Criterio3_UpdatesCostAndPrice_RecalculatesMargin()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = new InventarioService(context);
        var dto = new ActualizarPreciosDto
        {
            CostoUnitario = 20.00m,
            PrecioVenta = 80.00m
        };

        // Act
        var updated = await service.ActualizarPreciosAsync(2, dto);

        // Assert
        // Margen = (80 - 20) / 80 * 100 = 75.00%
        updated.CostoUnitario.Should().Be(20.00m);
        updated.PrecioVenta.Should().Be(80.00m);
        updated.MargenPorcentaje.Should().Be(75.00m);

        // Verificar persistencia
        var inDb = await context.Productos.FindAsync(2);
        inDb!.Costo.Should().Be(20.00m);
        inDb.PrecioVenta.Should().Be(80.00m);
    }

    [Fact]
    public async Task ActualizarPrecios_Regla2_NegativeMarginProtection_ThrowsInvalidOperationException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = new InventarioService(context);
        var dto = new ActualizarPreciosDto
        {
            CostoUnitario = 50.00m,
            PrecioVenta = 45.00m // Precio menor al costo
        };

        // Act
        var act = async () => await service.ActualizarPreciosAsync(2, dto);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*margen negativo*");
    }

    [Fact]
    public async Task RegistrarEntradaMercancia_Criterio2_AddsStockAndUpdatesLastCost()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = new InventarioService(context);
        var dto = new RegistrarEntradaDto
        {
            IdProveedor = 1,
            FolioFactura = "F-9021",
            TotalPagado = 650.00m,
            Lineas = new List<RegistrarEntradaLineaDto>
            {
                new()
                {
                    IdProducto = 5, // M&M's: stock inicial 5, costo inicial 12, precio 35
                    Cantidad = 50,
                    CostoUnitario = 13.00m // Total = 650
                }
            }
        };

        // Act
        var result = await service.RegistrarEntradaMercanciaAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.Proveedor.Should().Be("Distribuidora Central de Dulces");
        result.Factura.Should().Be("F-9021");
        result.TotalPagado.Should().Be(650.00m);

        // Verificar stock de producto actualizado (5 + 50 = 55) y costo actualizado a 13.00m
        var productoDb = await context.Productos.FindAsync(5);
        productoDb!.StockActual.Should().Be(55);
        productoDb.Costo.Should().Be(13.00m);

        // Verificar persistencia de factura y entrada
        var facturaDb = await context.FacturasProveedor.Include(f => f.Entradas).FirstOrDefaultAsync(f => f.Folio == "F-9021");
        facturaDb.Should().NotBeNull();
        facturaDb!.Entradas.Should().HaveCount(1);
        facturaDb.Entradas.First().CantidadStockAgregada.Should().Be(50);
    }

    [Fact]
    public async Task RegistrarEntradaMercancia_Regla1_StockFraccionado_MultipliesByEquivalenceFactor()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = new InventarioService(context);
        // Pizza: IdProducto = 4, FactorEquivalencia = 8, StockActual = 40
        var dto = new RegistrarEntradaDto
        {
            IdProveedor = 1,
            FolioFactura = "PIZZA-001",
            TotalPagado = 100.00m,
            Lineas = new List<RegistrarEntradaLineaDto>
            {
                new()
                {
                    IdProducto = 4,
                    Cantidad = 2, // 2 pizzas completas
                    CostoUnitario = 50.00m
                }
            }
        };

        // Act
        var result = await service.RegistrarEntradaMercanciaAsync(dto);

        // Assert
        // 2 pizzas * 8 rebanadas = +16 rebanadas agregadas al stock del POS
        // Stock final = 40 + 16 = 56
        var pizzaDb = await context.Productos.FindAsync(4);
        pizzaDb!.StockActual.Should().Be(56);

        var linea = result.Lineas.First();
        linea.CantidadComprada.Should().Be(2);
        linea.FactorEquivalencia.Should().Be(8);
        linea.CantidadStockAgregada.Should().Be(16);
    }

    [Fact]
    public async Task RegistrarEntradaMercancia_TotalMismatch_ThrowsInvalidOperationException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = new InventarioService(context);
        var dto = new RegistrarEntradaDto
        {
            IdProveedor = 1,
            FolioFactura = "F-ERR-1",
            TotalPagado = 1000.00m, // No coincide con la línea (50 * 10 = 500)
            Lineas = new List<RegistrarEntradaLineaDto>
            {
                new()
                {
                    IdProducto = 2,
                    Cantidad = 50,
                    CostoUnitario = 10.00m
                }
            }
        };

        // Act
        var act = async () => await service.RegistrarEntradaMercanciaAsync(dto);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*no coincide con la suma*");
    }

    [Fact]
    public async Task RegistrarEntradaMercancia_DuplicateFolioForSameProvider_ThrowsInvalidOperationException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = new InventarioService(context);
        var dto = new RegistrarEntradaDto
        {
            IdProveedor = 1,
            FolioFactura = "F-DUP-1",
            TotalPagado = 500.00m,
            Lineas = new List<RegistrarEntradaLineaDto>
            {
                new() { IdProducto = 2, Cantidad = 50, CostoUnitario = 10.00m }
            }
        };

        await service.RegistrarEntradaMercanciaAsync(dto);

        // Act - Segundo registro con mismo proveedor y folio
        var act = async () => await service.RegistrarEntradaMercanciaAsync(dto);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*ya fue registrada*");
    }

    [Fact]
    public async Task RegistrarEntradaMercancia_NewCostExceedsSalePrice_ThrowsInvalidOperationException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = new InventarioService(context);
        // M&M's: PrecioVenta = 35.00m
        var dto = new RegistrarEntradaDto
        {
            IdProveedor = 1,
            FolioFactura = "F-EXPENSIVE",
            TotalPagado = 400.00m,
            Lineas = new List<RegistrarEntradaLineaDto>
            {
                new()
                {
                    IdProducto = 5,
                    Cantidad = 10,
                    CostoUnitario = 40.00m // Costo mayor a PrecioVenta (35.00m)
                }
            }
        };

        // Act
        var act = async () => await service.RegistrarEntradaMercanciaAsync(dto);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*excede su precio de venta*");
    }

    [Fact]
    public async Task ObtenerInventarioMaestro_WithCategoryAndSearchFilters_FiltersCorrectly()
    {
        using var context = CreateInMemoryDbContext();
        var service = new InventarioService(context);

        var byCat = await service.ObtenerInventarioMaestroAsync("Snacks", "Palomitas");
        byCat.Should().HaveCount(1);
        byCat[0].Sku.Should().Be("7501001");

        var noMatch = await service.ObtenerInventarioMaestroAsync("Snacks", "Inexistente999");
        noMatch.Should().BeEmpty();
    }

    [Fact]
    public async Task ObtenerInsumoPorId_WhenExistsAndWhenNotFound_BehavesCorrectly()
    {
        using var context = CreateInMemoryDbContext();
        var service = new InventarioService(context);

        var found = await service.ObtenerInsumoPorIdAsync(2);
        found.Sku.Should().Be("7501001");

        var act = async () => await service.ObtenerInsumoPorIdAsync(999);
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task ActualizarPrecios_WhenProductNotFound_ThrowsKeyNotFoundException()
    {
        using var context = CreateInMemoryDbContext();
        var service = new InventarioService(context);

        var act = async () => await service.ActualizarPreciosAsync(999, new ActualizarPreciosDto { CostoUnitario = 10m, PrecioVenta = 20m });
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task ObtenerHistorialYProveedores_ReturnsExpectedLists()
    {
        using var context = CreateInMemoryDbContext();
        var service = new InventarioService(context);

        await service.RegistrarEntradaMercanciaAsync(new RegistrarEntradaDto
        {
            IdProveedor = 1,
            FolioFactura = "F-HIST-01",
            TotalPagado = 100m,
            Lineas = new List<RegistrarEntradaLineaDto>
            {
                new() { IdProducto = 2, Cantidad = 10, CostoUnitario = 10m }
            }
        });

        var historial = await service.ObtenerHistorialEntradasAsync(10);
        historial.Should().HaveCount(1);
        historial[0].Factura.Should().Be("F-HIST-01");
        historial[0].Lineas.Should().HaveCount(1);

        var proveedores = await service.ObtenerProveedoresAsync();
        proveedores.Should().NotBeEmpty();
        proveedores.Select(p => p.Nombre).Should().Contain("Distribuidora Central de Dulces");
    }

    [Fact]
    public async Task RegistrarEntradaMercancia_ValidationEdgeCases_ThrowsExpectedExceptions()
    {
        using var context = CreateInMemoryDbContext();
        var service = new InventarioService(context);

        var emptyLinesAct = async () => await service.RegistrarEntradaMercanciaAsync(new RegistrarEntradaDto
        {
            IdProveedor = 1,
            FolioFactura = "F-1",
            TotalPagado = 10m,
            Lineas = new List<RegistrarEntradaLineaDto>()
        });
        await emptyLinesAct.Should().ThrowAsync<ArgumentException>();

        var emptyFolioAct = async () => await service.RegistrarEntradaMercanciaAsync(new RegistrarEntradaDto
        {
            IdProveedor = 1,
            FolioFactura = "   ",
            TotalPagado = 10m,
            Lineas = new List<RegistrarEntradaLineaDto> { new() { IdProducto = 2, Cantidad = 1, CostoUnitario = 10m } }
        });
        await emptyFolioAct.Should().ThrowAsync<ArgumentException>();

        var missingProvAct = async () => await service.RegistrarEntradaMercanciaAsync(new RegistrarEntradaDto
        {
            IdProveedor = 999,
            FolioFactura = "F-2",
            TotalPagado = 10m,
            Lineas = new List<RegistrarEntradaLineaDto> { new() { IdProducto = 2, Cantidad = 1, CostoUnitario = 10m } }
        });
        await missingProvAct.Should().ThrowAsync<KeyNotFoundException>();

        var missingProdAct = async () => await service.RegistrarEntradaMercanciaAsync(new RegistrarEntradaDto
        {
            IdProveedor = 1,
            FolioFactura = "F-3",
            TotalPagado = 10m,
            Lineas = new List<RegistrarEntradaLineaDto> { new() { IdProducto = 999, Cantidad = 1, CostoUnitario = 10m } }
        });
        await missingProdAct.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task ObtenerInventarioPaginado_PaginatesBy15ItemsPerPage_AndClampsPage()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        for (int i = 1; i <= 35; i++)
        {
            context.Productos.Add(new Producto
            {
                IdProducto = 100 + i,
                CodigoBarras = $"7509{i:D3}",
                Nombre = $"Producto Extra {i:D2}",
                IdCategoria = 3, // Bebidas
                PrecioVenta = 40m + i,
                Costo = 15m,
                StockActual = i % 5 == 0 ? 0 : 25,
                StockMinimo = 10,
                FactorEquivalencia = 1,
                Activo = true
            });
        }
        await context.SaveChangesAsync();

        var service = new InventarioService(context);

        // Act: 4 base insumos + 35 nuevos = 39 insumos -> 3 páginas de 15 (15, 15, 9)
        var pag1 = await service.ObtenerInventarioPaginadoAsync(new InventarioFiltroDto { Pagina = 1, TamanoPagina = 15 });
        var pag3 = await service.ObtenerInventarioPaginadoAsync(new InventarioFiltroDto { Pagina = 3, TamanoPagina = 15 });
        var pagFueraRango = await service.ObtenerInventarioPaginadoAsync(new InventarioFiltroDto { Pagina = 99, TamanoPagina = 15 });

        // Assert
        pag1.TotalRegistros.Should().Be(39);
        pag1.TotalPaginas.Should().Be(3);
        pag1.PaginaActual.Should().Be(1);
        pag1.Items.Should().HaveCount(15);
        pag1.CategoriasDisponibles.Should().Contain(new[] { "Bebidas", "Comida", "Dulces", "Snacks" });

        pag3.PaginaActual.Should().Be(3);
        pag3.Items.Should().HaveCount(9);

        pagFueraRango.PaginaActual.Should().Be(3);
        pagFueraRango.Items.Should().HaveCount(9);
    }

    [Fact]
    public async Task ObtenerInventarioPaginado_FiltersByStockStatus_SearchAccentInsensitive_AndSorts()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        context.Productos.Add(new Producto
        {
            IdProducto = 200,
            CodigoBarras = "7508888",
            Nombre = "Bebida Agotada Especial",
            IdCategoria = 3,
            PrecioVenta = 50m,
            Costo = 20m,
            StockActual = 0,
            StockMinimo = 10,
            Activo = true
        });
        await context.SaveChangesAsync();

        var service = new InventarioService(context);

        // Act & Assert: búsqueda insensible a acentos y multi-palabra
        var busquedaAcento = await service.ObtenerInventarioPaginadoAsync(new InventarioFiltroDto
        {
            Busqueda = "palomitas clasicas"
        });
        busquedaAcento.TotalRegistros.Should().Be(1);
        busquedaAcento.Items[0].Sku.Should().Be("7501001");

        // Act & Assert: filtro de estado de stock "agotado"
        var agotados = await service.ObtenerInventarioPaginadoAsync(new InventarioFiltroDto
        {
            EstadoStock = "agotado"
        });
        agotados.TotalRegistros.Should().Be(1);
        agotados.Items[0].Sku.Should().Be("7508888");
        agotados.TotalAgotados.Should().Be(1);

        // Act & Assert: filtro "alerta" (Nachos 15<=15, M&M's 5<=10, Bebida 0<=10)
        var alerta = await service.ObtenerInventarioPaginadoAsync(new InventarioFiltroDto
        {
            EstadoStock = "alerta",
            OrdenarPor = "stock_asc"
        });
        alerta.TotalRegistros.Should().Be(3);
        alerta.Items.Select(i => i.StockActual).Should().BeInAscendingOrder();

        // Act & Assert: filtro "optimo" y ordenamiento por margen descendente
        var optimo = await service.ObtenerInventarioPaginadoAsync(new InventarioFiltroDto
        {
            EstadoStock = "optimo",
            OrdenarPor = "margen_desc"
        });
        optimo.TotalRegistros.Should().Be(2);
        optimo.Items[0].MargenPorcentaje.Should().BeGreaterThanOrEqualTo(optimo.Items[1].MargenPorcentaje);
    }
}
