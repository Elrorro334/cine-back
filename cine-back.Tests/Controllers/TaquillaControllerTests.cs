using cine_back.Controllers;
using Cine.Application.Dtos;
using Cine.Application.Dtos.Funcion;
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
    public void GetDulceria_TieneAllowAnonymousParaAccesoPublicoPwa()
    {
        var method = typeof(TaquillaController).GetMethod(nameof(TaquillaController.GetDulceria));
        method.Should().NotBeNull();
        var allowAnon = method!
            .GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute), inherit: true);
        allowAnon.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetCatalogoDulceriaPos_ReturnsOkWithCatalogo()
    {
        var catalogo = new CatalogoDulceriaPosDto
        {
            StockFraccionado = new StockFraccionadoDto
            {
                StockRebanadasActual = 56,
                PizzasCompletasEquivalentes = 7,
                EtiquetaMonitor = "Stock: 56 rebanadas (7 pizzas)"
            },
            SnacksYBebidas = new List<ProductoDto>
            {
                new ProductoDto { IdProducto = 1, Sku = "7501001", Nombre = "Palomitas Grandes", Precio = 50m, Stock = 40 }
            }
        };

        _taquillaServiceMock.Setup(s => s.ObtenerCatalogoDulceriaPosAsync()).ReturnsAsync(catalogo);

        var result = await _controller.GetCatalogoDulceriaPos();

        var ok = result as OkObjectResult;
        ok.Should().NotBeNull();
        ok!.StatusCode.Should().Be(200);
        ok.Value.Should().BeEquivalentTo(catalogo);
    }

    [Fact]
    public async Task BuscarProductoPorSku_WhenFound_ReturnsOk()
    {
        var prod = new ProductoDto { IdProducto = 1, Sku = "7501001", Nombre = "Palomitas Grandes", Precio = 50m, Stock = 25 };
        _taquillaServiceMock.Setup(s => s.BuscarProductoPorSkuAsync("7501001")).ReturnsAsync(prod);

        var result = await _controller.BuscarProductoPorSku("7501001");

        var ok = result as OkObjectResult;
        ok.Should().NotBeNull();
        ok!.StatusCode.Should().Be(200);
        ok.Value.Should().BeEquivalentTo(prod);
    }

    [Fact]
    public async Task BuscarProductoPorSku_WhenNotFound_ReturnsNotFound()
    {
        _taquillaServiceMock.Setup(s => s.BuscarProductoPorSkuAsync("0000000")).ReturnsAsync((ProductoDto?)null);

        var result = await _controller.BuscarProductoPorSku("0000000");

        var notFound = result as NotFoundObjectResult;
        notFound.Should().NotBeNull();
        notFound!.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task BuscarProductoPorSku_WhenEmpty_ReturnsBadRequest()
    {
        _taquillaServiceMock.Setup(s => s.BuscarProductoPorSkuAsync(""))
            .ThrowsAsync(new ArgumentException("SKU obligatorio"));

        var result = await _controller.BuscarProductoPorSku("");

        var badReq = result as BadRequestObjectResult;
        badReq.Should().NotBeNull();
        badReq!.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task BuscarProductoPorSku_WhenZeroStock_ReturnsConflict()
    {
        _taquillaServiceMock.Setup(s => s.BuscarProductoPorSkuAsync("7501001"))
            .ThrowsAsync(new InvalidOperationException("Inventario 0"));

        var result = await _controller.BuscarProductoPorSku("7501001");

        var conflict = result as ConflictObjectResult;
        conflict.Should().NotBeNull();
        conflict!.StatusCode.Should().Be(409);
    }

    [Fact]
    public async Task CalcularOrdenPos_WithValidRequest_ReturnsOk()
    {
        var req = new CalcularOrdenRequestDto { SkuAgregar = "7501001", CantidadAgregar = 1 };
        var res = new ResumenOrdenCalculadoDto { TotalVenta = 50m, TotalArticulos = 1 };

        _taquillaServiceMock.Setup(s => s.CalcularOrdenPosAsync(req)).ReturnsAsync(res);

        var result = await _controller.CalcularOrdenPos(req);

        var ok = result as OkObjectResult;
        ok.Should().NotBeNull();
        ok!.StatusCode.Should().Be(200);
        ok.Value.Should().BeEquivalentTo(res);
    }

    [Fact]
    public async Task CalcularOrdenPos_WhenThrowsExceptions_MapsExpectedHttpStatusCodes()
    {
        var reqBad = new CalcularOrdenRequestDto();
        _taquillaServiceMock.Setup(s => s.CalcularOrdenPosAsync(reqBad))
            .ThrowsAsync(new ArgumentException("Solicitud inválida"));
        var badRes = await _controller.CalcularOrdenPos(reqBad);
        (badRes as BadRequestObjectResult)!.StatusCode.Should().Be(400);

        var reqNotFound = new CalcularOrdenRequestDto { SkuAgregar = "9999999" };
        _taquillaServiceMock.Setup(s => s.CalcularOrdenPosAsync(reqNotFound))
            .ThrowsAsync(new KeyNotFoundException("SKU no encontrado"));
        var nfRes = await _controller.CalcularOrdenPos(reqNotFound);
        (nfRes as NotFoundObjectResult)!.StatusCode.Should().Be(404);

        var reqConflict = new CalcularOrdenRequestDto { SkuAgregar = "7501006", CantidadAgregar = 100 };
        _taquillaServiceMock.Setup(s => s.CalcularOrdenPosAsync(reqConflict))
            .ThrowsAsync(new InvalidOperationException("Stock fraccionado insuficiente"));
        var confRes = await _controller.CalcularOrdenPos(reqConflict);
        (confRes as ConflictObjectResult)!.StatusCode.Should().Be(409);
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
    public async Task RegistrarVenta_WhenThrowsInvalidOperationException_ReturnsConflict()
    {
        var req = new RegistrarVentaDto();
        _taquillaServiceMock.Setup(s => s.RegistrarVentaAsync(req))
            .ThrowsAsync(new InvalidOperationException("La butaca A1 ya se encuentra ocupada"));

        var result = await _controller.RegistrarVenta(req);

        var conflict = result as ConflictObjectResult;
        conflict.Should().NotBeNull();
        conflict!.StatusCode.Should().Be(409);
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

    [Fact]
    public async Task GetPreorden_WhenExpiredOrRedeemed_ReturnsConflict()
    {
        _taquillaServiceMock.Setup(s => s.ObtenerPreordenAsync("PRE-EXP"))
            .ThrowsAsync(new InvalidOperationException("La pre-orden 'PRE-EXP' ha expirado (15 días; vigencia máxima: 12 días)."));

        var result = await _controller.GetPreorden("PRE-EXP");

        var conflict = result as ConflictObjectResult;
        conflict.Should().NotBeNull();
        conflict!.StatusCode.Should().Be(409);
    }

    [Fact]
    public async Task GetPreorden_WhenArgumentException_ReturnsBadRequest()
    {
        _taquillaServiceMock.Setup(s => s.ObtenerPreordenAsync(""))
            .ThrowsAsync(new ArgumentException("Código obligatorio"));

        var result = await _controller.GetPreorden("");

        var bad = result as BadRequestObjectResult;
        bad.Should().NotBeNull();
        bad!.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task GetPreordenesActivas_ReturnsOkWithList()
    {
        var list = new List<PreordenDto>
        {
            new PreordenDto { IdPreorden = 1, Codigo = "PRE-XIL-001", Cliente = "Carlos", Total = 150m, VigenciaDias = 12, EstadoActivo = true }
        };
        _taquillaServiceMock.Setup(s => s.ObtenerPreordenesActivasAsync()).ReturnsAsync(list);

        var result = await _controller.GetPreordenesActivas();

        var ok = result as OkObjectResult;
        ok.Should().NotBeNull();
        ok!.StatusCode.Should().Be(200);
        ok.Value.Should().BeEquivalentTo(list);
    }

    [Fact]
    public async Task ValidarPreordenQr_MapsResponsesAndExceptionsCorrectly()
    {
        var dto = new PreordenDto { IdPreorden = 1, Codigo = "PRE-XIL-100", Total = 100m, VigenciaDias = 12, EstadoActivo = true };
        _taquillaServiceMock.Setup(s => s.ValidarPreordenQrAsync("PRE-XIL-100")).ReturnsAsync(dto);

        var okRes = await _controller.ValidarPreordenQr("PRE-XIL-100");
        (okRes as OkObjectResult)!.StatusCode.Should().Be(200);

        _taquillaServiceMock.Setup(s => s.ValidarPreordenQrAsync(""))
            .ThrowsAsync(new ArgumentException("Folio vacío"));
        var badRes = await _controller.ValidarPreordenQr("");
        (badRes as BadRequestObjectResult)!.StatusCode.Should().Be(400);

        _taquillaServiceMock.Setup(s => s.ValidarPreordenQrAsync("NO-EXISTE"))
            .ThrowsAsync(new KeyNotFoundException("No existe"));
        var nfRes = await _controller.ValidarPreordenQr("NO-EXISTE");
        (nfRes as NotFoundObjectResult)!.StatusCode.Should().Be(404);

        _taquillaServiceMock.Setup(s => s.ValidarPreordenQrAsync("PRE-CANJEADA"))
            .ThrowsAsync(new InvalidOperationException("Ya fue canjeada"));
        var confRes = await _controller.ValidarPreordenQr("PRE-CANJEADA");
        (confRes as ConflictObjectResult)!.StatusCode.Should().Be(409);
    }
}
