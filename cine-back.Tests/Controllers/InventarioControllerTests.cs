using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using cine_back.Controllers;
using Cine.Application.Dtos;
using Cine.Application.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace cine_back.Tests.Controllers;

public class InventarioControllerTests
{
    private readonly Mock<IInventarioService> _serviceMock;
    private readonly InventarioController _controller;

    public InventarioControllerTests()
    {
        _serviceMock = new Mock<IInventarioService>();
        _controller = new InventarioController(_serviceMock.Object);
    }

    [Fact]
    public async Task GetInsumos_ReturnsOkWithList()
    {
        // Arrange
        var list = new List<InsumoInventarioDto>
        {
            new()
            {
                IdProducto = 1,
                Sku = "7501001",
                Producto = "Palomitas Grandes",
                Categoria = "Snacks",
                StockActual = 100,
                MinimoAlerta = 20,
                CostoUnitario = 15.00m,
                PrecioVenta = 60.00m,
                MargenPorcentaje = 75.00m,
                EnAlerta = false
            }
        };

        _serviceMock.Setup(s => s.ObtenerInventarioMaestroAsync(null, null))
            .ReturnsAsync(list);

        // Act
        var result = await _controller.GetInsumos();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var value = okResult.Value.Should().BeAssignableTo<List<InsumoInventarioDto>>().Subject;
        value.Should().HaveCount(1);
    }

    [Fact]
    public async Task ActualizarPrecios_WhenValid_ReturnsOk()
    {
        // Arrange
        var dto = new ActualizarPreciosDto { CostoUnitario = 20.00m, PrecioVenta = 80.00m };
        var updated = new InsumoInventarioDto
        {
            IdProducto = 2,
            Sku = "7501002",
            Producto = "Refresco 1L",
            Categoria = "Bebidas",
            CostoUnitario = 20.00m,
            PrecioVenta = 80.00m,
            MargenPorcentaje = 75.00m
        };

        _serviceMock.Setup(s => s.ActualizarPreciosAsync(2, dto))
            .ReturnsAsync(updated);

        // Act
        var result = await _controller.ActualizarPrecios(2, dto);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var value = okResult.Value.Should().BeOfType<InsumoInventarioDto>().Subject;
        value.MargenPorcentaje.Should().Be(75.00m);
    }

    [Fact]
    public async Task ActualizarPrecios_WhenNegativeMargin_ReturnsBadRequest()
    {
        // Arrange
        var dto = new ActualizarPreciosDto { CostoUnitario = 50.00m, PrecioVenta = 40.00m };
        _serviceMock.Setup(s => s.ActualizarPreciosAsync(2, dto))
            .ThrowsAsync(new InvalidOperationException("Protección de margen negativo"));

        // Act
        var result = await _controller.ActualizarPrecios(2, dto);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task RegistrarEntrada_WhenValid_ReturnsCreated()
    {
        // Arrange
        var dto = new RegistrarEntradaDto
        {
            IdProveedor = 1,
            FolioFactura = "F-9021",
            TotalPagado = 500.00m,
            Lineas = new List<RegistrarEntradaLineaDto>
            {
                new() { IdProducto = 1, Cantidad = 50, CostoUnitario = 10.00m }
            }
        };

        var historialDto = new EntradaHistorialDto
        {
            IdFactura = 10,
            Proveedor = "Distribuidora Central de Dulces",
            Factura = "F-9021",
            TotalPagado = 500.00m,
            FechaRegistro = DateTime.UtcNow
        };

        _serviceMock.Setup(s => s.RegistrarEntradaMercanciaAsync(dto, It.IsAny<int?>()))
            .ReturnsAsync(historialDto);

        // Act
        var result = await _controller.RegistrarEntrada(dto);

        // Assert
        var statusResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(201);
        var value = statusResult.Value.Should().BeOfType<EntradaHistorialDto>().Subject;
        value.Factura.Should().Be("F-9021");
    }

    [Fact]
    public async Task GetProveedores_ReturnsOkWithList()
    {
        // Arrange
        var list = new List<ProveedorDto>
        {
            new() { IdProveedor = 1, Nombre = "Distribuidora Central de Dulces", Rfc = "DCD123", Telefono = "246123", Contacto = "Juan" }
        };

        _serviceMock.Setup(s => s.ObtenerProveedoresAsync())
            .ReturnsAsync(list);

        // Act
        var result = await _controller.GetProveedores();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var value = okResult.Value.Should().BeAssignableTo<List<ProveedorDto>>().Subject;
        value.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetInsumoPorId_WhenExists_ReturnsOk()
    {
        var dto = new InsumoInventarioDto { IdProducto = 5, Producto = "Pizza", Sku = "7501006", Categoria = "Comida", FactorEquivalencia = 8, Icono = "fa-burger" };
        _serviceMock.Setup(s => s.ObtenerInsumoPorIdAsync(5)).ReturnsAsync(dto);

        var result = await _controller.GetInsumoPorId(5);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetInsumoPorId_WhenNotFound_ReturnsNotFound()
    {
        _serviceMock.Setup(s => s.ObtenerInsumoPorIdAsync(999))
            .ThrowsAsync(new KeyNotFoundException("No encontrado"));

        var result = await _controller.GetInsumoPorId(999);

        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task ActualizarPrecios_WhenNotFound_ReturnsNotFound()
    {
        var dto = new ActualizarPreciosDto { CostoUnitario = 10m, PrecioVenta = 20m };
        _serviceMock.Setup(s => s.ActualizarPreciosAsync(999, dto))
            .ThrowsAsync(new KeyNotFoundException("No encontrado"));

        var result = await _controller.ActualizarPrecios(999, dto);

        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task ActualizarPrecios_WhenArgumentInvalid_ReturnsBadRequest()
    {
        var dto = new ActualizarPreciosDto { CostoUnitario = -1m, PrecioVenta = 20m };
        _serviceMock.Setup(s => s.ActualizarPreciosAsync(1, dto))
            .ThrowsAsync(new ArgumentException("Costo inválido"));

        var result = await _controller.ActualizarPrecios(1, dto);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task ActualizarPrecios_WhenModelStateInvalid_ReturnsBadRequest()
    {
        _controller.ModelState.AddModelError("CostoUnitario", "Requerido");
        var result = await _controller.ActualizarPrecios(1, new ActualizarPreciosDto());

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetHistorialEntradas_ReturnsOk()
    {
        var list = new List<EntradaHistorialDto>
        {
            new()
            {
                IdFactura = 1,
                Proveedor = "Prov",
                Factura = "F-1",
                TotalPagado = 100m,
                RegistradoPor = "admin",
                Lineas = new List<EntradaHistorialLineaDto>
                {
                    new() { IdEntrada = 1, IdProducto = 1, Producto = "P1", Sku = "S1", CantidadComprada = 2, FactorEquivalencia = 1, CantidadStockAgregada = 2, CostoUnitario = 50m, CostoTotal = 100m }
                }
            }
        };
        _serviceMock.Setup(s => s.ObtenerHistorialEntradasAsync(50)).ReturnsAsync(list);

        var result = await _controller.GetHistorialEntradas(50);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task RegistrarEntrada_WhenNotFound_ReturnsNotFound()
    {
        var dto = new RegistrarEntradaDto { IdProveedor = 99, FolioFactura = "F-1", TotalPagado = 10m };
        _serviceMock.Setup(s => s.RegistrarEntradaMercanciaAsync(dto, It.IsAny<int?>()))
            .ThrowsAsync(new KeyNotFoundException("Proveedor no existe"));

        var result = await _controller.RegistrarEntrada(dto);

        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task RegistrarEntrada_WhenInvalidOperation_ReturnsBadRequest()
    {
        var dto = new RegistrarEntradaDto { IdProveedor = 1, FolioFactura = "F-1", TotalPagado = 10m };
        _serviceMock.Setup(s => s.RegistrarEntradaMercanciaAsync(dto, It.IsAny<int?>()))
            .ThrowsAsync(new InvalidOperationException("Duplicado"));

        var result = await _controller.RegistrarEntrada(dto);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task RegistrarEntrada_WhenArgumentException_ReturnsBadRequest()
    {
        var dto = new RegistrarEntradaDto { IdProveedor = 1, FolioFactura = "", TotalPagado = 10m };
        _serviceMock.Setup(s => s.RegistrarEntradaMercanciaAsync(dto, It.IsAny<int?>()))
            .ThrowsAsync(new ArgumentException("Folio vacío"));

        var result = await _controller.RegistrarEntrada(dto);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetInsumosPaginado_ReturnsOkWithPaginatedDto()
    {
        // Arrange
        var paginado = new InventarioPaginadoDto
        {
            PaginaActual = 2,
            TamanoPagina = 15,
            TotalRegistros = 35,
            TotalPaginas = 3,
            TotalEnAlerta = 4,
            TotalAgotados = 1,
            CategoriasDisponibles = new List<string> { "Bebidas", "Comida", "Dulces", "Pizzas", "Snacks" },
            Items = new List<InsumoInventarioDto>()
        };

        _serviceMock
            .Setup(s => s.ObtenerInventarioPaginadoAsync(It.Is<InventarioFiltroDto>(f =>
                f.Categoria == "Snacks" &&
                f.Busqueda == "palomitas" &&
                f.EstadoStock == "alerta" &&
                f.OrdenarPor == "stock_asc" &&
                f.Pagina == 2 &&
                f.TamanoPagina == 15)))
            .ReturnsAsync(paginado);

        // Act
        var result = await _controller.GetInsumosPaginado("Snacks", "palomitas", "alerta", "stock_asc", 2, 15);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var value = okResult.Value.Should().BeOfType<InventarioPaginadoDto>().Subject;
        value.PaginaActual.Should().Be(2);
        value.TamanoPagina.Should().Be(15);
        value.TotalRegistros.Should().Be(35);
    }

    [Fact]
    public async Task RegistrarEntrada_WhenModelStateInvalid_ReturnsBadRequest()
    {
        _controller.ModelState.AddModelError("FolioFactura", "Requerido");
        var result = await _controller.RegistrarEntrada(new RegistrarEntradaDto());

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }
}
