using Cine.Application.Dtos;
using Cine.Application.Services;
using Cine.Domain.Entities;
using Cine.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace cine_back.Tests.Services;

public class TaquillaServiceTests
{
    private static CineDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<CineDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new CineDbContext(options);
    }

    [Fact]
    public async Task ObtenerCarteleraHoyAsync_FiltersOnlyTodayFunctions()
    {
        using var context = GetInMemoryDbContext();

        var sala = new Sala
        {
            IdSala = 1,
            NombreSala = "Sala 1",
            CapacidadTotal = 100,
            TipoPantalla = "MacroXE"
        };
        context.Salas.Add(sala);

        var peliculaHoy = new Pelicula
        {
            IdPelicula = 1,
            Titulo = "Pelicula De Hoy",
            Sinopsis = "Sinopsis de prueba",
            Duracion = 120,
            Director = "Director A",
            Clasificacion = "+13"
        };

        var peliculaAyer = new Pelicula
        {
            IdPelicula = 2,
            Titulo = "Pelicula De Ayer",
            Sinopsis = "Sinopsis de prueba",
            Duracion = 90,
            Director = "Director B",
            Clasificacion = "+13"
        };

        context.Peliculas.AddRange(peliculaHoy, peliculaAyer);

        context.Funcions.Add(new Funcion
        {
            IdFuncion = 10,
            IdPelicula = 1,
            FechaFuncion = DateTime.Today,
            HoraInicio = DateTime.Today.AddHours(15),
            Precio = 75.00m,
            IdSalaNavigation = sala
        });

        context.Funcions.Add(new Funcion
        {
            IdFuncion = 11,
            IdPelicula = 2,
            FechaFuncion = DateTime.Today.AddDays(-1),
            HoraInicio = DateTime.Today.AddDays(-1).AddHours(18),
            Precio = 75.00m,
            IdSalaNavigation = sala
        });

        await context.SaveChangesAsync();

        var service = new TaquillaService(context);

        var result = await service.ObtenerCarteleraHoyAsync(null);

        result.Should().HaveCount(1);
        result[0].Titulo.Should().Be("Pelicula De Hoy");
        result[0].Funciones.Should().HaveCount(1);
    }

    [Fact]
    public async Task RegistrarPeliculaDesdeTmdbAsync_StoresOnlyRelativePosterPathAndTruncatesSynopsis()
    {
        using var context = GetInMemoryDbContext();
        var service = new TaquillaService(context);

        var dto = new RegistrarPeliculaDto
        {
            Titulo = "Batman 2026",
            Sinopsis = new string('A', 150),
            Duracion = 140,
            Clasificacion = "B",
            Director = "Matt Reeves",
            PosterPath = "https://image.tmdb.org/t/p/w500/batman2026.jpg",
            Precio = 65.00m,
            Horario = "17:30"
        };

        var result = await service.RegistrarPeliculaDesdeTmdbAsync(dto);

        result.Should().NotBeNull();
        result.Titulo.Should().Be("Batman 2026");
        result.poster_image.Should().Be("/batman2026.jpg");
        result.poster_image.Should().NotContain("http");
        result.Sinopsis.Length.Should().BeLessThanOrEqualTo(100);
        result.Sinopsis.Should().EndWith("...");

        var peliculaEnDb = await context.Peliculas.FirstAsync(p => p.IdPelicula == result.IdPelicula);
        peliculaEnDb.PosterImage.Should().Be("/batman2026.jpg");
        peliculaEnDb.Sinopsis.Length.Should().BeLessThanOrEqualTo(100);
    }

    [Fact]
    public async Task RegistrarPeliculaDesdeTmdbAsync_WithMultipleHorariosAndHttpUrl_CreatesFunctionsForRange()
    {
        using var context = GetInMemoryDbContext();
        var service = new TaquillaService(context);

        var dto = new RegistrarPeliculaDto
        {
            Titulo = "Dune Parte 2",
            Sinopsis = "",
            Duracion = 165,
            Clasificacion = "B15",
            Director = "Denis Villeneuve",
            PosterPath = "https://images.unsplash.com/photo-12345.jpg",
            IdSala = 1,
            Precio = 60.00m,
            Horarios = new List<string> { "14:00", "18:30", "invalid" },
            FechaInicio = DateTime.Today,
            FechaFin = DateTime.Today.AddDays(1)
        };

        var result = await service.RegistrarPeliculaDesdeTmdbAsync(dto);

        result.Should().NotBeNull();
        result.poster_image.Should().Be("/photo-12345.jpg");
        result.Funciones.Should().HaveCount(2);
    }

    [Fact]
    public async Task RegistrarPeliculaDesdeTmdbAsync_WhenTitleAlreadyExists_ReusesPeliculaWithoutDuplicating()
    {
        using var context = GetInMemoryDbContext();
        var service = new TaquillaService(context);

        var dto1 = new RegistrarPeliculaDto
        {
            Titulo = "Slender Man",
            Sinopsis = "El hombre más pálido",
            Duracion = 93,
            Clasificacion = "B15",
            Director = "Sylvain White",
            PosterPath = "/slender.jpg",
            Precio = 50.00m,
            Horario = "16:00"
        };

        var dto2 = new RegistrarPeliculaDto
        {
            Titulo = "Slender Man",
            Sinopsis = "El hombre más pálido reestreno",
            Duracion = 93,
            Clasificacion = "B15",
            Director = "Sylvain White",
            PosterPath = "/slender.jpg",
            Precio = 50.00m,
            Horario = "19:00"
        };

        var res1 = await service.RegistrarPeliculaDesdeTmdbAsync(dto1);
        var res2 = await service.RegistrarPeliculaDesdeTmdbAsync(dto2);

        res1.IdPelicula.Should().Be(res2.IdPelicula);
        var peliculasCount = await context.Peliculas.CountAsync(p => p.Titulo == "Slender Man");
        peliculasCount.Should().Be(1);
    }

    [Fact]
    public async Task EliminarPeliculaAsync_RemovesPeliculaAndCascadeFunctions()
    {
        using var context = GetInMemoryDbContext();
        var service = new TaquillaService(context);

        var dto = new RegistrarPeliculaDto
        {
            Titulo = "Pelicula Caducada",
            Sinopsis = "Para dar de baja",
            Duracion = 100,
            Clasificacion = "B",
            Director = "Director X",
            PosterPath = "/caducada.jpg",
            Precio = 50.00m,
            Horario = "14:00"
        };

        var creada = await service.RegistrarPeliculaDesdeTmdbAsync(dto);

        var eliminada = await service.EliminarPeliculaAsync(creada.IdPelicula);

        eliminada.Should().BeTrue();
        var existe = await context.Peliculas.AnyAsync(p => p.IdPelicula == creada.IdPelicula);
        existe.Should().BeFalse();
        var funcionesExisten = await context.Funcions.AnyAsync(f => f.IdPelicula == creada.IdPelicula);
        funcionesExisten.Should().BeFalse();
    }

    [Fact]
    public async Task EliminarPeliculaAsync_WhenNotExists_ReturnsFalse()
    {
        using var context = GetInMemoryDbContext();
        var service = new TaquillaService(context);

        var result = await service.EliminarPeliculaAsync(99999);
        result.Should().BeFalse();
    }

    [Fact]
    public async Task EliminarPeliculaAsync_WithPreordensAndButacas_CleansAllCascade()
    {
        using var context = GetInMemoryDbContext();

        var pelicula = new Pelicula
        {
            IdPelicula = 200,
            Titulo = "Con Dependencias",
            Director = "Director Test",
            Sinopsis = "Sinopsis Test",
            Duracion = 120,
            Clasificacion = "B",
            PosterImage = "/test.jpg"
        };
        var funcion = new Funcion { IdFuncion = 201, IdPelicula = 200, FechaFuncion = DateTime.Today, Precio = 50 };
        var butacaEstado = new EstadoButacaFuncion { IdEstado = 202, IdFuncion = 201 };
        var preorden = new Preorden { IdPreorden = 203, IdFuncion = 201 };
        var boleto = new DetallePreordenBoleto { IdDetalle = 204, IdPreorden = 203 };

        context.Peliculas.Add(pelicula);
        context.Funcions.Add(funcion);
        context.EstadoButacaFuncions.Add(butacaEstado);
        context.Preordens.Add(preorden);
        context.DetallePreordenBoletos.Add(boleto);
        await context.SaveChangesAsync();

        var service = new TaquillaService(context);
        var result = await service.EliminarPeliculaAsync(200);

        result.Should().BeTrue();
        (await context.Peliculas.AnyAsync(p => p.IdPelicula == 200)).Should().BeFalse();
        (await context.Funcions.AnyAsync(f => f.IdFuncion == 201)).Should().BeFalse();
        (await context.EstadoButacaFuncions.AnyAsync(b => b.IdEstado == 202)).Should().BeFalse();
        (await context.Preordens.AnyAsync(p => p.IdPreorden == 203)).Should().BeFalse();
        (await context.DetallePreordenBoletos.AnyAsync(d => d.IdDetalle == 204)).Should().BeFalse();
    }

    [Fact]
    public async Task BuscarTmdbAsync_WithValidResults_ReturnsMappedList()
    {
        using var context = GetInMemoryDbContext();
        var fakeJson = "{\"results\":[{\"id\":100,\"title\":\"Batman\",\"original_title\":\"The Batman\",\"overview\":\"Hero\",\"poster_path\":\"/bat.jpg\",\"release_date\":\"2022-03-04\",\"vote_average\":7.8}]}";

        var fakeHandler = new FakeHttpMessageHandler((req) => new HttpResponseMessage
        {
            StatusCode = System.Net.HttpStatusCode.OK,
            Content = new StringContent(fakeJson, System.Text.Encoding.UTF8, "application/json")
        });

        var httpClient = new HttpClient(fakeHandler);
        var service = new TaquillaService(context, httpClient);

        var result = await service.BuscarTmdbAsync("Batman");

        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].TmdbId.Should().Be(100);
        result[0].Titulo.Should().Be("Batman");
        result[0].ReleaseYear.Should().Be("2022");
    }

    [Fact]
    public async Task BuscarTmdbAsync_WithEmptyQuery_ReturnsEmptyList()
    {
        using var context = GetInMemoryDbContext();
        var service = new TaquillaService(context);

        var result = await service.BuscarTmdbAsync("   ");
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task BuscarTmdbAsync_WithHttpError_ReturnsEmptyList()
    {
        using var context = GetInMemoryDbContext();
        var fakeHandler = new FakeHttpMessageHandler((req) => new HttpResponseMessage
        {
            StatusCode = System.Net.HttpStatusCode.InternalServerError
        });

        var service = new TaquillaService(context, new HttpClient(fakeHandler));
        var result = await service.BuscarTmdbAsync("Spider");
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task BuscarTmdbAsync_WhenThrows_ReturnsEmptyList()
    {
        using var context = GetInMemoryDbContext();
        var fakeHandler = new FakeHttpMessageHandler((req) => throw new HttpRequestException("Network error"));

        var service = new TaquillaService(context, new HttpClient(fakeHandler));
        var result = await service.BuscarTmdbAsync("Spider");
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ObtenerDetalleTmdbAsync_WithValidId_ReturnsDetailDto()
    {
        using var context = GetInMemoryDbContext();
        var fakeJson = "{\"id\":155,\"title\":\"The Dark Knight\",\"original_title\":\"The Dark Knight\",\"tagline\":\"Why so serious?\",\"overview\":\"Batman vs Joker\",\"runtime\":152,\"poster_path\":\"/dark.jpg\",\"backdrop_path\":\"/dark_bg.jpg\",\"release_date\":\"2008-07-18\",\"vote_average\":9.0,\"genres\":[{\"name\":\"Action\"}],\"credits\":{\"cast\":[{\"name\":\"Christian Bale\"}],\"crew\":[{\"name\":\"Christopher Nolan\",\"job\":\"Director\"}]},\"release_dates\":{\"results\":[{\"iso_3166_1\":\"MX\",\"release_dates\":[{\"certification\":\"PG-13\"}]}]},\"videos\":{\"results\":[{\"site\":\"YouTube\",\"type\":\"Trailer\",\"key\":\"EXeTwQWrcwY\"}]}}";

        var fakeHandler = new FakeHttpMessageHandler((req) => new HttpResponseMessage
        {
            StatusCode = System.Net.HttpStatusCode.OK,
            Content = new StringContent(fakeJson, System.Text.Encoding.UTF8, "application/json")
        });

        var service = new TaquillaService(context, new HttpClient(fakeHandler));
        var result = await service.ObtenerDetalleTmdbAsync(155);

        result.Should().NotBeNull();
        result!.TmdbId.Should().Be(155);
        result.Titulo.Should().Be("The Dark Knight");
        result.Eslogan.Should().Be("Why so serious?");
        result.GenerosCompletos.Should().Be("Action");
        result.BackdropPath.Should().Be("/dark_bg.jpg");
        result.Duracion.Should().Be(152);
        result.Director.Should().Be("Christopher Nolan");
        result.Clasificacion.Should().Be("B15");
        result.TrailerKey.Should().Be("EXeTwQWrcwY");
    }

    [Fact]
    public async Task ObtenerDetalleTmdbAsync_WithInvalidId_ReturnsNull()
    {
        using var context = GetInMemoryDbContext();
        var service = new TaquillaService(context);

        var result = await service.ObtenerDetalleTmdbAsync(0);
        result.Should().BeNull();
    }

    [Fact]
    public async Task ObtenerDetalleTmdbAsync_WithNotFoundResponse_ReturnsNull()
    {
        using var context = GetInMemoryDbContext();
        var fakeHandler = new FakeHttpMessageHandler((req) => new HttpResponseMessage
        {
            StatusCode = System.Net.HttpStatusCode.NotFound
        });

        var service = new TaquillaService(context, new HttpClient(fakeHandler));
        var result = await service.ObtenerDetalleTmdbAsync(999);
        result.Should().BeNull();
    }

    [Fact]
    public async Task SincronizarCatalogoBaseAsync_RegistersMoviesWithFallbackWhenOffline()
    {
        using var context = GetInMemoryDbContext();
        var fakeHandler = new FakeHttpMessageHandler((req) => new HttpResponseMessage
        {
            StatusCode = System.Net.HttpStatusCode.NotFound
        });

        var service = new TaquillaService(context, new HttpClient(fakeHandler));
        var result = await service.SincronizarCatalogoBaseAsync();

        result.Should().HaveCount(8);
        (await context.Peliculas.CountAsync()).Should().Be(8);
    }

    [Fact]
    public async Task ObtenerPromocionesAsync_ReturnsPromocionesWithFontAwesomeIcons()
    {
        using var context = GetInMemoryDbContext();
        var service = new TaquillaService(context);

        var promos = await service.ObtenerPromocionesAsync();

        promos.Should().HaveCount(3);
        promos[0].Icono.Should().StartWith("fa-");
    }

    [Fact]
    public async Task ObtenerButacasFuncionAsync_WhenExists_ReturnsSeatMap()
    {
        using var context = GetInMemoryDbContext();
        var sala = new Sala { IdSala = 1, NombreSala = "Sala 1", CapacidadTotal = 48, TipoPantalla = "Tradicional" };
        var funcion = new Funcion { IdFuncion = 10, IdPelicula = 1, IdSala = 1, FechaFuncion = DateTime.Today, HoraInicio = DateTime.Today.AddHours(16), Precio = 50m, IdSalaNavigation = sala };
        var b1 = new Butaca { IdButaca = 1, IdSala = 1, Fila = "A", Numero = 1 };
        var b2 = new Butaca { IdButaca = 2, IdSala = 1, Fila = "A", Numero = 2 };
        var estado = new EstadoButacaFuncion { IdEstado = 1, IdFuncion = 10, IdButaca = 2, Estato = true };

        context.Salas.Add(sala);
        context.Funcions.Add(funcion);
        context.Butacas.AddRange(b1, b2);
        context.EstadoButacaFuncions.Add(estado);
        await context.SaveChangesAsync();

        var service = new TaquillaService(context);
        var result = await service.ObtenerButacasFuncionAsync(10);

        result.IdFuncion.Should().Be(10);
        result.NombreSala.Should().Be("Sala 1");
        result.Precio.Should().Be(50m);
        result.Butacas.Should().HaveCount(2);
        result.Butacas.First(b => b.Codigo == "A1").EstaOcupada.Should().BeFalse();
        result.Butacas.First(b => b.Codigo == "A2").EstaOcupada.Should().BeTrue();
    }

    [Fact]
    public async Task ObtenerButacasFuncionAsync_WhenNotFound_ThrowsArgumentException()
    {
        using var context = GetInMemoryDbContext();
        var service = new TaquillaService(context);

        Func<Task> act = async () => await service.ObtenerButacasFuncionAsync(999);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task ObtenerProductosDulceriaAsync_ExcludesTaquillaAndMapsImages()
    {
        using var context = GetInMemoryDbContext();
        var catSnacks = new Categorium { IdCategoria = 1, Nombre = "Snacks" };
        var catTaquilla = new Categorium { IdCategoria = 2, Nombre = "Taquilla" };

        context.Categoria.AddRange(catSnacks, catTaquilla);
        context.Productos.AddRange(
            new Producto { IdProducto = 1, IdCategoria = 1, CodigoBarras = "7501001", Nombre = "Palomitas", PrecioVenta = 50m, StockActual = 20, IdCategoriaNavigation = catSnacks },
            new Producto { IdProducto = 2, IdCategoria = 1, CodigoBarras = "CUSTOM", Nombre = "Dulce", PrecioVenta = 20m, StockActual = 10, IdCategoriaNavigation = catSnacks },
            new Producto { IdProducto = 3, IdCategoria = 2, CodigoBarras = "BOL-GEN", Nombre = "Boleto General", PrecioVenta = 50m, StockActual = 999, IdCategoriaNavigation = catTaquilla }
        );
        await context.SaveChangesAsync();

        var service = new TaquillaService(context);
        var result = await service.ObtenerProductosDulceriaAsync();

        result.Should().HaveCount(2);
        result.Should().Contain(p => p.Sku == "7501001");
        result.Should().Contain(p => p.Sku == "CUSTOM");
    }

    [Fact]
    public async Task RegistrarVentaAsync_WithEmptyItems_ThrowsArgumentException()
    {
        using var context = GetInMemoryDbContext();
        var service = new TaquillaService(context);

        Func<Task> act = async () => await service.RegistrarVentaAsync(new RegistrarVentaDto());
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task RegistrarVentaAsync_WithDulceriaAndBoletos_PersistsSaleDecrementsStockAndOccupiesSeats()
    {
        using var context = GetInMemoryDbContext();

        var sala = new Sala { IdSala = 1, NombreSala = "Sala 1", CapacidadTotal = 48, TipoPantalla = "2D" };
        var funcion = new Funcion { IdFuncion = 10, IdPelicula = 1, IdSala = 1, FechaFuncion = DateTime.Today, HoraInicio = DateTime.Today.AddHours(18), Precio = 50m };
        var b1 = new Butaca { IdButaca = 1, IdSala = 1, Fila = "B", Numero = 1 };
        var b2 = new Butaca { IdButaca = 2, IdSala = 1, Fila = "B", Numero = 2 };
        var estadoPrevio = new EstadoButacaFuncion { IdEstado = 1, IdFuncion = 10, IdButaca = 2, Estato = false };

        var prodDulce = new Producto { IdProducto = 1, IdCategoria = 1, CodigoBarras = "7501001", Nombre = "Palomitas", PrecioVenta = 50m, StockActual = 10 };
        var prodBoleto = new Producto { IdProducto = 2, IdCategoria = 2, CodigoBarras = "BOL-GEN", Nombre = "Boleto General", PrecioVenta = 50m, StockActual = 500 };

        context.Salas.Add(sala);
        context.Funcions.Add(funcion);
        context.Butacas.AddRange(b1, b2);
        context.EstadoButacaFuncions.Add(estadoPrevio);
        context.Productos.AddRange(prodDulce, prodBoleto);
        await context.SaveChangesAsync();

        var service = new TaquillaService(context);

        var dto = new RegistrarVentaDto
        {
            IdVendedor = 1,
            MetodoPago = "Tarjeta",
            Items = new List<RegistrarVentaItemDto>
            {
                new RegistrarVentaItemDto
                {
                    Tipo = "dulceria",
                    IdProducto = 1,
                    Sku = "7501001",
                    Nombre = "Palomitas",
                    Cantidad = 2,
                    Precio = 50m
                },
                new RegistrarVentaItemDto
                {
                    Tipo = "boleto",
                    IdFuncion = 10,
                    Nombre = "Boleto General",
                    Cantidad = 2,
                    Precio = 50m,
                    Asientos = new List<string> { "B1", "B2" }
                }
            }
        };

        var confirmada = await service.RegistrarVentaAsync(dto);

        confirmada.Should().NotBeNull();
        confirmada.Folio.Should().StartWith("VT-");
        confirmada.Total.Should().Be(200m);
        confirmada.MetodoPago.Should().Be("Tarjeta");

        var updatedProd = await context.Productos.FirstAsync(p => p.IdProducto == 1);
        updatedProd.StockActual.Should().Be(8);

        var asientosOcupados = await context.EstadoButacaFuncions.Where(e => e.IdFuncion == 10 && e.Estato).ToListAsync();
        asientosOcupados.Should().HaveCount(2);
    }

    [Fact]
    public async Task ObtenerPreordenAsync_WithValidAndInvalidQr_ReturnsExpectedResult()
    {
        using var context = GetInMemoryDbContext();

        var cliente = new Cliente { IdCliente = 1, Nombre = "Ana López", Correo = "ana@test.com", NumeroTelefono = "5551234567" };
        var pelicula = new Pelicula { IdPelicula = 1, Titulo = "Avatar", Sinopsis = "Sinopsis", Duracion = 180, Clasificacion = "B", Director = "Cameron" };
        var funcion = new Funcion { IdFuncion = 5, IdPelicula = 1, IdSala = 1, FechaFuncion = DateTime.Today, HoraInicio = DateTime.Today.AddHours(19), Precio = 50m, IdPeliculaNavigation = pelicula };
        var prodCombo = new Producto { IdProducto = 10, IdCategoria = 1, CodigoBarras = "COMBO01", Nombre = "Combo Pareja", PrecioVenta = 110m, StockActual = 10 };

        var preorden = new Preorden
        {
            IdPreorden = 1,
            IdCliente = 1,
            IdFuncion = 5,
            FechaCreacion = DateTime.Today,
            EstadoActivo = true,
            Vigencia = 8,
            CodigoQr = "XIL-2026-001",
            IdClienteNavigation = cliente,
            IdFuncionNavigation = funcion
        };

        var detBoleto = new DetallePreordenBoleto { IdDetalle = 1, IdPreorden = 1, Caantidad = 2, Subtotal = 100m };
        var detDulce = new DetallePreordenDulcerium { IdDetalle = 1, IdPreorden = 1, IdProducto = 10, Cantidad = 1, IdProductoNavigation = prodCombo };

        context.Clientes.Add(cliente);
        context.Peliculas.Add(pelicula);
        context.Funcions.Add(funcion);
        context.Productos.Add(prodCombo);
        context.Preordens.Add(preorden);
        context.DetallePreordenBoletos.Add(detBoleto);
        context.DetallePreordenDulceria.Add(detDulce);
        await context.SaveChangesAsync();

        var service = new TaquillaService(context);

        var emptyRes = await service.ObtenerPreordenAsync("  ");
        emptyRes.Should().BeNull();

        var notFoundRes = await service.ObtenerPreordenAsync("NO-EXISTE");
        notFoundRes.Should().BeNull();

        var validRes = await service.ObtenerPreordenAsync("XIL-2026-001");
        validRes.Should().NotBeNull();
        validRes!.Cliente.Should().Be("Ana López");
        validRes.Pelicula.Should().Be("Avatar");
        validRes.BoletosGeneral.Should().Be(2);
        validRes.Combos.Should().HaveCount(1);
        validRes.Total.Should().Be(210m);
    }

    private class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;
        public FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_handler(request));
        }
    }
}
