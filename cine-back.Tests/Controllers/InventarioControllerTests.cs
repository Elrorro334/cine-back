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
            new() { IdProveedor = 1, Nombre = "Distribuidora Central de Dulces" }
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
}
