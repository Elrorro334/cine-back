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
                    new FuncionHorarioDto { IdFuncion = 10, Hora = "15:00", NombreSala = "Sala 1", Precio = 50m }
                }
            }
        };

        _taquillaServiceMock.Setup(service => service.ObtenerCarteleraHoyAsync(null))
            .ReturnsAsync(mockCartelera);

        var result = await _controller.GetCartelera(null);

        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);

        var returnValue = okResult.Value as List<PeliculaCarteleraDto>;
        returnValue.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetCartelera_WithSearchFilter_PassesFilterToService()
    {
        string busqueda = "Spider";
        var mockCartelera = new List<PeliculaCarteleraDto>
        {
            new PeliculaCarteleraDto { IdPelicula = 1, Titulo = "Spider-Man: Un Nuevo Día" }
        };

        _taquillaServiceMock.Setup(service => service.ObtenerCarteleraHoyAsync(busqueda))
            .ReturnsAsync(mockCartelera);

        var result = await _controller.GetCartelera(busqueda);

        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();

        _taquillaServiceMock.Verify(s => s.ObtenerCarteleraHoyAsync(busqueda), Times.Once);
    }

    [Fact]
    public async Task GetButacasFuncion_WhenExists_ReturnsOk()
    {
        var dto = new FuncionButacasDto
        {
            IdFuncion = 5,
            IdSala = 1,
            NombreSala = "Sala 1",
            Precio = 50m,
            Butacas = new List<ButacaDto>
            {
                new ButacaDto { IdButaca = 1, Fila = "A", Numero = 1, Codigo = "A1", EstaOcupada = false }
            }
        };

        _taquillaServiceMock.Setup(s => s.ObtenerButacasFuncionAsync(5)).ReturnsAsync(dto);

        var result = await _controller.GetButacasFuncion(5);

        var ok = result as OkObjectResult;
        ok.Should().NotBeNull();
        ok!.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task GetButacasFuncion_WhenNotFound_ReturnsNotFound()
    {
        _taquillaServiceMock.Setup(s => s.ObtenerButacasFuncionAsync(999))
            .ThrowsAsync(new ArgumentException("No se encontró la función"));

        var result = await _controller.GetButacasFuncion(999);

        var notFound = result as NotFoundObjectResult;
        notFound.Should().NotBeNull();
        notFound!.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task GetDulceria_ReturnsOkWithProductos()
    {
        var productos = new List<ProductoDto>
        {
            new ProductoDto { IdProducto = 1, Sku = "7501001", Nombre = "Palomitas", Categoria = "Snacks", Precio = 50m, Stock = 20, Img = "url" }
        };

        _taquillaServiceMock.Setup(s => s.ObtenerProductosDulceriaAsync()).ReturnsAsync(productos);

        var result = await _controller.GetDulceria();

        var ok = result as OkObjectResult;
        ok.Should().NotBeNull();
        ok!.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task RegistrarVenta_WithValidDto_ReturnsOk()
    {
        var req = new RegistrarVentaDto
        {
            IdVendedor = 1,
            MetodoPago = "Efectivo",
            Items = new List<RegistrarVentaItemDto>
            {
                new RegistrarVentaItemDto { Tipo = "dulceria", IdProducto = 1, Nombre = "Palomitas", Cantidad = 1, Precio = 50m }
            }
        };

        var resp = new VentaConfirmadaDto
        {
            IdVenta = 100,
            Folio = "VT-00100",
            Total = 50m,
            MetodoPago = "Efectivo",
            FechaVenta = DateTime.Now,
            Items = req.Items
        };

        _taquillaServiceMock.Setup(s => s.RegistrarVentaAsync(req)).ReturnsAsync(resp);

        var result = await _controller.RegistrarVenta(req);

        var ok = result as OkObjectResult;
        ok.Should().NotBeNull();
        ok!.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task RegistrarVenta_WhenThrowsArgumentException_ReturnsBadRequest()
    {
        var req = new RegistrarVentaDto();
        _taquillaServiceMock.Setup(s => s.RegistrarVentaAsync(req))
            .ThrowsAsync(new ArgumentException("Sin items"));

        var result = await _controller.RegistrarVenta(req);

        var badReq = result as BadRequestObjectResult;
        badReq.Should().NotBeNull();
        badReq!.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task GetPreorden_WhenExists_ReturnsOk()
    {
        var preorden = new PreordenDto
        {
            IdPreorden = 1,
            Codigo = "XIL-001",
            Cliente = "Juan",
            Telefono = "555",
            Pelicula = "Batman",
            Horario = "16:00",
            IdFuncion = 1,
            BoletosGeneral = 2,
            PrecioBoleto = 50m,
            Asientos = new List<string> { "C4" },
            Combos = new List<PreordenComboDto> { new PreordenComboDto { Nombre = "Combo 1", Cantidad = 1, Precio = 100m } },
            Total = 200m,
            FechaCreacion = "2026-10-20",
            VigenciaDias = 8,
            EstadoActivo = true
        };

        _taquillaServiceMock.Setup(s => s.ObtenerPreordenAsync("XIL-001")).ReturnsAsync(preorden);

        var result = await _controller.GetPreorden("XIL-001");

        var ok = result as OkObjectResult;
        ok.Should().NotBeNull();
        ok!.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task GetPreorden_WhenNull_ReturnsNotFound()
    {
        _taquillaServiceMock.Setup(s => s.ObtenerPreordenAsync("INVALID")).ReturnsAsync((PreordenDto?)null);

        var result = await _controller.GetPreorden("INVALID");

        var notFound = result as NotFoundObjectResult;
        notFound.Should().NotBeNull();
        notFound!.StatusCode.Should().Be(404);
    }
}
