using System.Collections.Generic;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using cine_back.Controllers;
using Cine.Application.Dtos;
using Cine.Application.Exceptions;
using Cine.Application.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace cine_back.Tests.Controllers;

public class ConfiguracionControllerTests
{
    private readonly Mock<IConfiguracionAdminService> _serviceMock;
    private readonly ConfiguracionController _controller;

    public ConfiguracionControllerTests()
    {
        _serviceMock = new Mock<IConfiguracionAdminService>();
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "7"),
            new Claim(ClaimTypes.Role, "Administrador")
        }, "TestAuth"));

        _controller = new ConfiguracionController(_serviceMock.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = user,
                    Connection = { RemoteIpAddress = IPAddress.Parse("192.168.1.50") }
                }
            }
        };
    }

    [Fact]
    public async Task GetSeguridad_UsesXForwardedForAndReturns200Ok()
    {
        var expected = new ConfiguracionSeguridadDto
        {
            Id = 1,
            IpAutorizada = "192.168.1.200",
            IpClienteDetectada = "192.168.1.20"
        };

        _serviceMock
            .Setup(s => s.ObtenerConfiguracionSeguridadAsync("192.168.1.20"))
            .ReturnsAsync(expected);

        var result = await _controller.GetSeguridad("192.168.1.20, 10.0.0.1");

        var ok = result.Result as OkObjectResult;
        ok.Should().NotBeNull();
        ok!.StatusCode.Should().Be(200);
        ok.Value.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task ActualizarSeguridad_ValidRequest_Returns200Ok()
    {
        var dto = new ActualizarConfiguracionSeguridadDto
        {
            IpAutorizada = "192.168.1.200",
            HorarioInicioCajeros = "08:00:00",
            HorarioFinCajeros = "23:00:00",
            AutorizarExcepcionHorasExtras = true
        };

        var updated = new ConfiguracionSeguridadDto
        {
            Id = 1,
            IpAutorizada = "192.168.1.200",
            AutorizarExcepcionHorasExtras = true
        };

        _serviceMock
            .Setup(s => s.ActualizarConfiguracionSeguridadAsync(dto, "192.168.1.50", 7))
            .ReturnsAsync(updated);

        var result = await _controller.ActualizarSeguridad(dto);

        var ok = result.Result as OkObjectResult;
        ok.Should().NotBeNull();
        ok!.StatusCode.Should().Be(200);
        ok.Value.Should().BeEquivalentTo(updated);
    }

    [Fact]
    public async Task ActualizarSeguridad_DifferentSubnet_Returns409Conflict()
    {
        var dto = new ActualizarConfiguracionSeguridadDto
        {
            IpAutorizada = "10.10.10.10",
            HorarioInicioCajeros = "08:00:00",
            HorarioFinCajeros = "23:00:00",
            ConfirmarCambioRed = false
        };

        _serviceMock
            .Setup(s => s.ActualizarConfiguracionSeguridadAsync(dto, "192.168.1.50", 7))
            .ThrowsAsync(new SubredDiferenteException("192.168.1.50", "10.10.10.10", "Subred distinta"));

        var result = await _controller.ActualizarSeguridad(dto);

        var conflict = result.Result as ObjectResult;
        conflict.Should().NotBeNull();
        conflict!.StatusCode.Should().Be(409);
    }

    [Fact]
    public async Task ActualizarSeguridad_ArgumentException_Returns400BadRequest()
    {
        var dto = new ActualizarConfiguracionSeguridadDto { IpAutorizada = "invalida" };

        _serviceMock
            .Setup(s => s.ActualizarConfiguracionSeguridadAsync(dto, "192.168.1.50", 7))
            .ThrowsAsync(new System.ArgumentException("IP inválida"));

        var result = await _controller.ActualizarSeguridad(dto);

        var bad = result.Result as BadRequestObjectResult;
        bad.Should().NotBeNull();
        bad!.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task ActualizarSeguridad_InvalidModelState_Returns400BadRequest()
    {
        _controller.ModelState.AddModelError("IpAutorizada", "Requerida");
        var result = await _controller.ActualizarSeguridad(new ActualizarConfiguracionSeguridadDto());

        var bad = result.Result as BadRequestObjectResult;
        bad.Should().NotBeNull();
        bad!.StatusCode.Should().Be(400);
    }
}
