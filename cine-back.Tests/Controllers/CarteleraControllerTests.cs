using cine_back.Controllers;
using Cine.Application.Dtos;
using Cine.Application.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace cine_back.Tests.Controllers;

public class CarteleraControllerTests
{
    private readonly Mock<ITaquillaService> _taquillaServiceMock;
    private readonly CarteleraController _controller;

    public CarteleraControllerTests()
    {
        _taquillaServiceMock = new Mock<ITaquillaService>();
        _controller = new CarteleraController(_taquillaServiceMock.Object);
    }

    [Fact]
    public async Task GetCartelera_ReturnsOkWithCartelera()
    {
        // Arrange
        var mockCartelera = new List<PeliculaCarteleraDto>
        {
            new PeliculaCarteleraDto
            {
                IdPelicula = 1,
                Titulo = "Spider-Man",
                poster_image = "/spiderman.jpg"
            }
        };

        _taquillaServiceMock.Setup(s => s.ObtenerCarteleraHoyAsync(null))
            .ReturnsAsync(mockCartelera);

        // Act
        var result = await _controller.GetCartelera(null);

        // Assert
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);

        var list = okResult.Value as List<PeliculaCarteleraDto>;
        list.Should().HaveCount(1);
        list![0].Titulo.Should().Be("Spider-Man");
    }

    [Fact]
    public async Task BuscarTmdb_WithValidQuery_ReturnsOkWithResults()
    {
        // Arrange
        var mockResults = new List<TmdbSearchResultDto>
        {
            new TmdbSearchResultDto
            {
                TmdbId = 155,
                Titulo = "Batman",
                PosterPath = "/batman.jpg"
            }
        };

        _taquillaServiceMock.Setup(s => s.BuscarTmdbAsync("Batman"))
            .ReturnsAsync(mockResults);

        // Act
        var result = await _controller.BuscarTmdb("Batman");

        // Assert
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);

        var list = okResult.Value as List<TmdbSearchResultDto>;
        list.Should().HaveCount(1);
    }

    [Fact]
    public async Task BuscarTmdb_WithEmptyQuery_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.BuscarTmdb("   ");

        // Assert
        var badRequest = result as BadRequestObjectResult;
        badRequest.Should().NotBeNull();
        badRequest!.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task DetalleTmdb_WhenFound_ReturnsOk()
    {
        // Arrange
        var mockDetail = new TmdbMovieDetailDto
        {
            TmdbId = 155,
            Titulo = "Batman",
            PosterPath = "/batman.jpg"
        };

        _taquillaServiceMock.Setup(s => s.ObtenerDetalleTmdbAsync(155))
            .ReturnsAsync(mockDetail);

        // Act
        var result = await _controller.DetalleTmdb(155);

        // Assert
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task DetalleTmdb_WhenNotFound_ReturnsNotFound()
    {
        // Arrange
        _taquillaServiceMock.Setup(s => s.ObtenerDetalleTmdbAsync(999))
            .ReturnsAsync((TmdbMovieDetailDto?)null);

        // Act
        var result = await _controller.DetalleTmdb(999);

        // Assert
        var notFoundResult = result as NotFoundObjectResult;
        notFoundResult.Should().NotBeNull();
        notFoundResult!.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task RegistrarPelicula_WithValidDto_ReturnsCreated()
    {
        // Arrange
        var dto = new RegistrarPeliculaDto
        {
            Titulo = "Avatar 3",
            PosterPath = "/avatar3.jpg"
        };

        var created = new PeliculaCarteleraDto
        {
            IdPelicula = 50,
            Titulo = "Avatar 3",
            poster_image = "/avatar3.jpg"
        };

        _taquillaServiceMock.Setup(s => s.RegistrarPeliculaDesdeTmdbAsync(dto))
            .ReturnsAsync(created);

        // Act
        var result = await _controller.RegistrarPelicula(dto);

        // Assert
        var createdResult = result as CreatedResult;
        createdResult.Should().NotBeNull();
        createdResult!.StatusCode.Should().Be(201);
    }

    [Fact]
    public async Task RegistrarPelicula_WithEmptyTitle_ReturnsBadRequest()
    {
        // Arrange
        var dto = new RegistrarPeliculaDto { Titulo = "" };

        // Act
        var result = await _controller.RegistrarPelicula(dto);

        // Assert
        var badRequest = result as BadRequestObjectResult;
        badRequest.Should().NotBeNull();
    }

    [Fact]
    public async Task EliminarPelicula_WhenSuccessful_ReturnsOk()
    {
        // Arrange
        _taquillaServiceMock.Setup(s => s.EliminarPeliculaAsync(50))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.EliminarPelicula(50);

        // Assert
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task EliminarPelicula_WhenNotFound_ReturnsNotFound()
    {
        // Arrange
        _taquillaServiceMock.Setup(s => s.EliminarPeliculaAsync(999))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.EliminarPelicula(999);

        // Assert
        var notFoundResult = result as NotFoundObjectResult;
        notFoundResult.Should().NotBeNull();
        notFoundResult!.StatusCode.Should().Be(404);
    }
}
