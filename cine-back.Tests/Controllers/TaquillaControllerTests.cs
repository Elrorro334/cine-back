using cine_back.Controllers;
using Cine.Application.Dtos;
using Cine.Application.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace cine_back.Tests.Controllers;

public class TaquillaControllerTests
{
    private readonly Mock<ITaquillaService> _taquillaServiceMock;
    private readonly TaquillaController _controller;

    public TaquillaControllerTests()
    {
        _taquillaServiceMock = new Mock<ITaquillaService>();
        _controller = new TaquillaController(_taquillaServiceMock.Object);
    }

    [Fact]
    public async Task GetCartelera_WithoutFilter_ReturnsOkResult_WithPeliculas()
    {
        // Arrange
        var mockCartelera = new List<PeliculaCarteleraDto>
        {
            new PeliculaCarteleraDto
            {
                IdPelicula = 1,
                Titulo = "Spider-Man: Un Nuevo Día",
                Clasificacion = "+18",
                Director = "Sam Raimi",
                Funciones = new List<FuncionHorarioDto>
                {
                    new FuncionHorarioDto { IdFuncion = 10, Hora = "15:00", NombreSala = "Sala 1" }
                }
            }
        };

        _taquillaServiceMock.Setup(service => service.ObtenerCarteleraHoyAsync(null))
            .ReturnsAsync(mockCartelera);

        // Act
        var result = await _controller.GetCartelera(null);

        // Assert
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);

        var returnValue = okResult.Value as List<PeliculaCarteleraDto>;
        returnValue.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetCartelera_WithSearchFilter_PassesFilterToService()
    {
        // Arrange
        string busqueda = "Spider";
        var mockCartelera = new List<PeliculaCarteleraDto>
        {
            new PeliculaCarteleraDto { IdPelicula = 1, Titulo = "Spider-Man: Un Nuevo Día" }
        };

        _taquillaServiceMock.Setup(service => service.ObtenerCarteleraHoyAsync(busqueda))
            .ReturnsAsync(mockCartelera);

        // Act
        var result = await _controller.GetCartelera(busqueda);

        // Assert
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();

        _taquillaServiceMock.Verify(s => s.ObtenerCarteleraHoyAsync(busqueda), Times.Once);
    }
}