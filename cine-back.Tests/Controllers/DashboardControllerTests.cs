using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using cine_back.Controllers;
using Cine.Application.Dtos;
using Cine.Application.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace cine_back.Tests.Controllers;

public class DashboardControllerTests
{
    private readonly Mock<IDashboardFinancieroService> _serviceMock;
    private readonly DashboardController _controller;

    public DashboardControllerTests()
    {
        _serviceMock = new Mock<IDashboardFinancieroService>();
        _controller = new DashboardController(_serviceMock.Object);
    }

    [Fact]
    public async Task GetResumen_ReturnsOkWithDashboardDto()
    {
        // Arrange
        var mockDto = new DashboardFinancieroDto
        {
            Periodo = new PeriodoDashboardDto
            {
                FechaInicio = DateTime.Today.AddDays(-3),
                FechaFin = DateTime.Today.AddDays(3),
                DescripcionSemana = "Semana Cinematográfica Actual"
            },
            Kpis = new KpiFinancieroDto
            {
                TotalIngresos = 5000.00m,
                TotalTaquilla = 3500.00m,
                BoletosVendidos = 45,
                TotalDulceria = 1500.00m,
                ProductosDulceriaVendidos = 20,
                TotalTransacciones = 25
            },
            Transacciones = new List<TransaccionDetalleDto>
            {
                new TransaccionDetalleDto
                {
                    IdVenta = 1,
                    FechaHora = DateTime.Now,
                    Cajero = "Carlos",
                    Total = 150.00m,
                    MetodoPago = "Tarjeta"
                }
            }
        };

        _serviceMock.Setup(s => s.ObtenerDashboardAsync(It.IsAny<FiltroDashboardDto>()))
            .ReturnsAsync(mockDto);

        // Act
        var result = await _controller.GetResumen(null, null, null);

        // Assert
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);

        var data = okResult.Value as DashboardFinancieroDto;
        data.Should().NotBeNull();
        data!.Kpis.TotalIngresos.Should().Be(5000.00m);
        data.Transacciones.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetSemanaActual_ReturnsOkWithSemanaDto()
    {
        // Arrange
        var mockSemana = new SemanaCinematograficaDto
        {
            FechaInicio = new DateTime(2026, 10, 15),
            FechaFin = new DateTime(2026, 10, 21),
            Descripcion = "Semana Cinematográfica del 15/10/2026 al 21/10/2026"
        };

        _serviceMock.Setup(s => s.ObtenerSemanaActualAsync())
            .ReturnsAsync(mockSemana);

        // Act
        var result = await _controller.GetSemanaActual();

        // Assert
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);

        var data = okResult.Value as SemanaCinematograficaDto;
        data.Should().NotBeNull();
        data!.Descripcion.Should().Contain("15/10/2026");
    }

    [Fact]
    public async Task ExportarTransacciones_ReturnsFileResultWithCsvMimeAndFilename()
    {
        // Arrange
        byte[] csvBytes = Encoding.UTF8.GetBytes("Folio,Fecha,Total\n1,2026-10-20,100");
        _serviceMock.Setup(s => s.ExportarTransaccionesCsvAsync(It.IsAny<FiltroDashboardDto>()))
            .ReturnsAsync(csvBytes);

        // Act
        var result = await _controller.ExportarTransacciones(null, null, null);

        // Assert
        var fileResult = result as FileContentResult;
        fileResult.Should().NotBeNull();
        fileResult!.ContentType.Should().Be("text/csv; charset=utf-8");
        fileResult.FileDownloadName.Should().StartWith("reporte_financiero_cine_xilotzin_");
        fileResult.FileDownloadName.Should().EndWith(".csv");
        fileResult.FileContents.Should().BeEquivalentTo(csvBytes);
    }
}
