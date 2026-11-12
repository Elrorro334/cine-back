using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using cine_back.Controllers;
using Cine.Application.Dtos;
using Cine.Application.Interfaces;
using Cine.Application.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace cine_back.Tests.Controllers;

public class TematicasControllerTests
{
    private readonly Mock<IConfiguracionAdminService> _serviceMock;
    private readonly TematicaEventBroadcaster _broadcaster;
    private readonly TematicasController _controller;

    public TematicasControllerTests()
    {
        _serviceMock = new Mock<IConfiguracionAdminService>();
        _broadcaster = new TematicaEventBroadcaster();
        _controller = new TematicasController(_serviceMock.Object, _broadcaster)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }

    [Fact]
    public async Task GetTematicas_Returns200Ok()
    {
        var lista = new List<TematicaSitioDto>
        {
            new() { Id = 1, NombreTematica = "Xilotzin Clásico", ClaveIdentificadora = "theme-classic", EsActiva = true }
        };
        _serviceMock.Setup(s => s.ObtenerTematicasAsync()).ReturnsAsync(lista);

        var result = await _controller.GetTematicas();

        var ok = result.Result as OkObjectResult;
        ok.Should().NotBeNull();
        ok!.StatusCode.Should().Be(200);
        ok.Value.Should().BeEquivalentTo(lista);
    }

    [Fact]
    public async Task GetTematicaActiva_WhenExists_Returns200Ok()
    {
        var activa = new TematicaSitioDto
        {
            Id = 2,
            NombreTematica = "Noche de Terror",
            ClaveIdentificadora = "theme-halloween",
            RutaArchivoCss = "/css/themes/halloween.min.css",
            EsActiva = true
        };
        _serviceMock.Setup(s => s.ObtenerTematicaActivaAsync()).ReturnsAsync(activa);

        var result = await _controller.GetTematicaActiva();

        var ok = result.Result as OkObjectResult;
        ok.Should().NotBeNull();
        ok!.StatusCode.Should().Be(200);
        ok.Value.Should().BeEquivalentTo(activa);
    }

    [Fact]
    public async Task GetTematicaActiva_WhenNull_Returns404NotFound()
    {
        _serviceMock.Setup(s => s.ObtenerTematicaActivaAsync()).ReturnsAsync((TematicaSitioDto?)null);

        var result = await _controller.GetTematicaActiva();

        var notFound = result.Result as NotFoundObjectResult;
        notFound.Should().NotBeNull();
        notFound!.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task ActivarTematica_WhenExists_Returns200Ok()
    {
        var activada = new TematicaSitioDto
        {
            Id = 2,
            NombreTematica = "Noche de Terror",
            ClaveIdentificadora = "theme-halloween",
            RutaArchivoCss = "/css/themes/halloween.min.css",
            EsActiva = true
        };
        _serviceMock.Setup(s => s.ActivarTematicaAsync(2)).ReturnsAsync(activada);

        var result = await _controller.ActivarTematica(2);

        var ok = result.Result as OkObjectResult;
        ok.Should().NotBeNull();
        ok!.StatusCode.Should().Be(200);
        ok.Value.Should().BeEquivalentTo(activada);
    }

    [Fact]
    public async Task ActivarTematica_WhenNotFound_Returns404NotFound()
    {
        _serviceMock.Setup(s => s.ActivarTematicaAsync(99))
            .ThrowsAsync(new KeyNotFoundException("No encontrada"));

        var result = await _controller.ActivarTematica(99);

        var notFound = result.Result as NotFoundObjectResult;
        notFound.Should().NotBeNull();
        notFound!.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task StreamTematicaActiva_SendsInitialThemeAndBroadcastUpdate()
    {
        var initial = new TematicaSitioDto
        {
            Id = 1,
            NombreTematica = "Xilotzin Clásico",
            ClaveIdentificadora = "theme-classic",
            RutaArchivoCss = "/css/themes/classic.min.css",
            EsActiva = true
        };
        var updated = new TematicaSitioDto
        {
            Id = 2,
            NombreTematica = "Noche de Terror",
            ClaveIdentificadora = "theme-halloween",
            RutaArchivoCss = "/css/themes/halloween.min.css",
            EsActiva = true
        };

        _serviceMock.Setup(s => s.ObtenerTematicaActivaAsync()).ReturnsAsync(initial);

        var bodyStream = new MemoryStream();
        _controller.HttpContext.Response.Body = bodyStream;

        using var cts = new CancellationTokenSource();
        var streamTask = _controller.StreamTematicaActiva(cts.Token);

        await Task.Delay(50);
        await _broadcaster.PublishAsync(updated);
        await Task.Delay(50);
        await cts.CancelAsync();

        await streamTask;

        bodyStream.Position = 0;
        using var reader = new StreamReader(bodyStream, Encoding.UTF8);
        var sseOutput = await reader.ReadToEndAsync();

        sseOutput.Should().Contain("event: theme-updated");
        sseOutput.Should().Contain("/css/themes/classic.min.css");
        sseOutput.Should().Contain("/css/themes/halloween.min.css");
    }
}
