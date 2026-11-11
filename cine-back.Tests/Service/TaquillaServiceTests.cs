using Cine.Application.Dtos;
using Cine.Application.Exceptions;
using Cine.Application.Services;
using Cine.Domain.Entities;
using Cine.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Text.RegularExpressions;

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
    public async Task RegistrarVentaAsync_WhenSeatAlreadyOccupied_ThrowsInvalidOperationException()
    {
        using var context = GetInMemoryDbContext();

        var sala = new Sala { IdSala = 1, NombreSala = "Sala 1", CapacidadTotal = 48, TipoPantalla = "2D" };
        var funcion = new Funcion { IdFuncion = 10, IdPelicula = 1, IdSala = 1, FechaFuncion = DateTime.Today, HoraInicio = DateTime.Today.AddHours(18), Precio = 50m };
        var b1 = new Butaca { IdButaca = 1, IdSala = 1, Fila = "B", Numero = 1 };
        var estadoOcupado = new EstadoButacaFuncion { IdEstado = 1, IdFuncion = 10, IdButaca = 1, Estato = true };

        context.Salas.Add(sala);
        context.Funcions.Add(funcion);
        context.Butacas.Add(b1);
        context.EstadoButacaFuncions.Add(estadoOcupado);
        await context.SaveChangesAsync();

        var service = new TaquillaService(context);

        var dto = new RegistrarVentaDto
        {
            IdVendedor = 1,
            MetodoPago = "Efectivo",
            Items = new List<RegistrarVentaItemDto>
            {
                new RegistrarVentaItemDto
                {
                    Tipo = "boleto",
                    IdFuncion = 10,
                    Nombre = "Boleto General",
                    Cantidad = 1,
                    Precio = 50m,
                    Asientos = new List<string> { "B1" }
                }
            }
        };

        Func<Task> act = async () => await service.RegistrarVentaAsync(dto);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*ya se encuentra ocupada*");
    }

    [Fact]
    public async Task RegistrarVentaAsync_WhenDuplicateSeatsInRequest_ThrowsArgumentException()
    {
        using var context = GetInMemoryDbContext();

        var sala = new Sala { IdSala = 1, NombreSala = "Sala 1", CapacidadTotal = 48, TipoPantalla = "2D" };
        var funcion = new Funcion { IdFuncion = 10, IdPelicula = 1, IdSala = 1, FechaFuncion = DateTime.Today, HoraInicio = DateTime.Today.AddHours(18), Precio = 50m };
        var b1 = new Butaca { IdButaca = 1, IdSala = 1, Fila = "B", Numero = 1 };

        context.Salas.Add(sala);
        context.Funcions.Add(funcion);
        context.Butacas.Add(b1);
        await context.SaveChangesAsync();

        var service = new TaquillaService(context);

        var dto = new RegistrarVentaDto
        {
            IdVendedor = 1,
            MetodoPago = "Efectivo",
            Items = new List<RegistrarVentaItemDto>
            {
                new RegistrarVentaItemDto
                {
                    Tipo = "boleto",
                    IdFuncion = 10,
                    Nombre = "Boleto General",
                    Cantidad = 2,
                    Precio = 50m,
                    Asientos = new List<string> { "B1", "B1" }
                }
            }
        };

        Func<Task> act = async () => await service.RegistrarVentaAsync(dto);
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*duplicados*");
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
        validRes.Asientos.Should().BeEmpty("las butacas se asignan en taquilla al canjear el QR");
        validRes.Combos.Should().HaveCount(1);
        validRes.Total.Should().Be(210m);
    }

    [Fact]
    public async Task CrearPreordenAsync_WithValidData_CreatesAndReturnsPreorden()
    {
        var context = GetInMemoryDbContext();
        var sala = new Sala { IdSala = 1, NombreSala = "Sala 1", CapacidadTotal = 100, TipoPantalla = "2D" };
        var pelicula = new Pelicula { IdPelicula = 301, Titulo = "La posesion de la momia", Director = "Director Test", Sinopsis = "Sinopsis Test" };
        var funcion = new Funcion
        {
            IdFuncion = 302,
            IdPelicula = 301,
            IdSala = 1,
            FechaFuncion = DateTime.Today,
            IdSalaNavigation = sala,
            IdPeliculaNavigation = pelicula,
            HoraInicio = DateTime.Today.AddHours(14).AddMinutes(30),
            Precio = 50m
        };
        context.Salas.Add(sala);
        var combo = new Producto
        {
            IdProducto = 303,
            CodigoBarras = "COMBO01",
            Nombre = "Combo Pareja",
            PrecioVenta = 110m
        };

        context.Peliculas.Add(pelicula);
        context.Funcions.Add(funcion);
        context.Productos.Add(combo);
        await context.SaveChangesAsync();

        var service = new TaquillaService(context);
        var dto = new CrearPreordenDto
        {
            IdFuncion = 302,
            Boletos = 2,
            NombreCliente = "Alberto Cruz",
            TelefonoCliente = "5512345678",
            Combos = new List<CrearPreordenItemDulceriaDto>
            {
                new CrearPreordenItemDulceriaDto { IdProducto = 303, Cantidad = 1 }
            }
        };

        var result = await service.CrearPreordenAsync(dto);

        result.Should().NotBeNull();
        result.Codigo.Should().StartWith("PRE-XIL-");
        result.Cliente.Should().Be("Alberto Cruz");
        result.Telefono.Should().Be("5512345678");
        result.Pelicula.Should().Be("La posesion de la momia");
        result.BoletosGeneral.Should().Be(2);
        result.PrecioBoleto.Should().Be(50m);
        result.Combos.Should().HaveCount(1);
        result.Total.Should().Be(210m); // 2*50 + 110 = 210
        result.VigenciaDias.Should().Be(12);
        result.EstadoActivo.Should().BeTrue();

        var dbPreorden = await context.Preordens.FirstOrDefaultAsync(p => p.CodigoQr == result.Codigo);
        dbPreorden.Should().NotBeNull();
        dbPreorden!.Vigencia.Should().Be(12);
    }

    [Fact]
    public async Task CrearPreordenAsync_WithExistingClient_ReusesClientAndUpdatesName()
    {
        var context = GetInMemoryDbContext();
        var sala2 = new Sala { IdSala = 1, NombreSala = "Sala 1", CapacidadTotal = 100, TipoPantalla = "2D" };
        var pelicula = new Pelicula { IdPelicula = 310, Titulo = "Spiderman", Director = "Director Test", Sinopsis = "Sinopsis Test" };
        var funcion = new Funcion
        {
            IdFuncion = 311,
            IdPelicula = 310,
            IdSala = 1,
            FechaFuncion = DateTime.Today,
            IdSalaNavigation = sala2,
            IdPeliculaNavigation = pelicula,
            HoraInicio = DateTime.Today.AddHours(16),
            Precio = 60m
        };
        context.Salas.Add(sala2);
        var clienteExistente = new Cliente
        {
            IdCliente = 400,
            Nombre = "Antiguo Nombre",
            NumeroTelefono = "5599887766",
            Correo = "5599887766@xilotzin.local"
        };

        context.Peliculas.Add(pelicula);
        context.Funcions.Add(funcion);
        context.Clientes.Add(clienteExistente);
        await context.SaveChangesAsync();

        var service = new TaquillaService(context);
        var dto = new CrearPreordenDto
        {
            IdFuncion = 311,
            Boletos = 3,
            NombreCliente = "Nuevo Nombre",
            TelefonoCliente = "5599887766"
        };

        var result = await service.CrearPreordenAsync(dto);

        result.Should().NotBeNull();
        result.Cliente.Should().Be("Nuevo Nombre");
        result.Total.Should().Be(180m); // 3*60 = 180

        var clientesCount = await context.Clientes.CountAsync(c => c.NumeroTelefono == "5599887766");
        clientesCount.Should().Be(1);
    }

    [Theory]
    [InlineData("7731234567", "7731234567")] // Tula de Allende, Hidalgo
    [InlineData("+52 773 123 4567", "7731234567")] // Formato con +52 y espacios
    [InlineData("+52 (773) 123-4567", "7731234567")] // Formato con paréntesis y guiones
    [InlineData("+52 1 773 123 4567", "7731234567")] // Formato antiguo +52 1
    [InlineData("3312345678", "3312345678")] // Guadalajara
    [InlineData("8112345678", "8112345678")] // Monterrey
    public async Task CrearPreordenAsync_WithDifferentMexicanAreaCodes_NormalizesAndSucceeds(string telefonoInput, string telefonoEsperado)
    {
        var context = GetInMemoryDbContext();
        var pelicula = new Pelicula
        {
            IdPelicula = 312,
            Titulo = "Avatar Ladas",
            Director = "Director Test",
            Sinopsis = "Sinopsis Test",
            Duracion = 120
        };
        var sala = new Sala { IdSala = 2, NombreSala = "Sala 2", CapacidadTotal = 100, TipoPantalla = "2D" };
        var funcion = new Funcion
        {
            IdFuncion = 312,
            IdPelicula = 312,
            IdSala = 2,
            FechaFuncion = DateTime.Today,
            HoraInicio = DateTime.Today.AddHours(18),
            IdSalaNavigation = sala,
            IdPeliculaNavigation = pelicula,
            Precio = 60m
        };

        context.Salas.Add(sala);
        context.Peliculas.Add(pelicula);
        context.Funcions.Add(funcion);
        await context.SaveChangesAsync();

        var service = new TaquillaService(context);
        var dto = new CrearPreordenDto
        {
            IdFuncion = 312,
            Boletos = 2,
            NombreCliente = "Cliente México",
            TelefonoCliente = telefonoInput
        };

        var result = await service.CrearPreordenAsync(dto);

        result.Should().NotBeNull();
        result.Telefono.Should().Be(telefonoEsperado);
        var cliente = await context.Clientes.FirstOrDefaultAsync(c => c.NumeroTelefono == telefonoEsperado);
        cliente.Should().NotBeNull();
    }

    [Theory]
    [InlineData(0, 2, "Juan", "5512345678")] // IdFuncion inválido
    [InlineData(1, 0, "Juan", "5512345678")] // Boletos < 1
    [InlineData(1, 11, "Juan", "5512345678")] // Boletos > 10
    [InlineData(1, 2, "", "5512345678")] // Nombre vacío
    [InlineData(1, 2, "Ab", "5512345678")] // Nombre muy corto
    [InlineData(1, 2, "Juan", "1234567890")] // Teléfono que no empieza con 55
    [InlineData(1, 2, "Juan", "551234567")] // Teléfono de 9 dígitos
    [InlineData(1, 2, "Juan", "55123456789")] // Teléfono de 11 dígitos
    [InlineData(1, 2, "Juan", "551234567A")] // Caracter no numérico
    public async Task CrearPreordenAsync_WithInvalidInputs_ThrowsArgumentException(
        int idFuncion, int boletos, string nombre, string telefono)
    {
        var context = GetInMemoryDbContext();
        var service = new TaquillaService(context);

        var dto = new CrearPreordenDto
        {
            IdFuncion = idFuncion,
            Boletos = boletos,
            NombreCliente = nombre,
            TelefonoCliente = telefono
        };

        Func<Task> act = async () => await service.CrearPreordenAsync(dto);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task CrearPreordenAsync_WhenDtoNull_ThrowsArgumentException()
    {
        var context = GetInMemoryDbContext();
        var service = new TaquillaService(context);

        Func<Task> act = async () => await service.CrearPreordenAsync(null!);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task CrearPreordenAsync_WhenFuncionNotFound_ThrowsArgumentException()
    {
        var context = GetInMemoryDbContext();
        var service = new TaquillaService(context);

        var dto = new CrearPreordenDto
        {
            IdFuncion = 9999,
            Boletos = 2,
            NombreCliente = "Cliente Prueba",
            TelefonoCliente = "5511223344"
        };

        Func<Task> act = async () => await service.CrearPreordenAsync(dto);
        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*No se encontró la función*");
    }

    [Fact]
    public async Task ObtenerTrailerPeliculaAsync_WhenMovieNotFound_ReturnsNull()
    {
        using var context = GetInMemoryDbContext();
        var service = new TaquillaService(context);

        var result = await service.ObtenerTrailerPeliculaAsync(9999);
        result.Should().BeNull();
    }

    [Fact]
    public async Task ObtenerTrailerPeliculaAsync_WhenFoundAndTmdbReturnsVideos_SelectsValidYouTubeVideo()
    {
        using var context = GetInMemoryDbContext();
        var pelicula = new Pelicula
        {
            IdPelicula = 500,
            Titulo = "Dragon 3",
            Sinopsis = "Sinopsis de dragon",
            Duracion = 100,
            Director = "Dean DeBlois",
            Clasificacion = "A"
        };
        context.Peliculas.Add(pelicula);
        await context.SaveChangesAsync();

        var searchJson = "{\"results\":[{\"id\":166428,\"title\":\"Dragon 3\"}]}";
        var videosJson = "{\"results\":[{\"site\":\"YouTube\",\"type\":\"Trailer\",\"key\":\"invalidKey\",\"name\":\"Deleted Trailer\"},{\"site\":\"YouTube\",\"type\":\"Trailer\",\"key\":\"kN3lE7rpggQ\",\"name\":\"Trailer Oficial Español\"}]}";

        var fakeHandler = new FakeHttpMessageHandler((req) =>
        {
            var uri = req.RequestUri?.ToString() ?? "";
            if (uri.Contains("api.themoviedb.org/3/search/movie"))
            {
                return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
                {
                    Content = new StringContent(searchJson, System.Text.Encoding.UTF8, "application/json")
                };
            }
            if (uri.Contains("api.themoviedb.org/3/movie/166428/videos"))
            {
                return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
                {
                    Content = new StringContent(videosJson, System.Text.Encoding.UTF8, "application/json")
                };
            }
            if (uri.Contains("youtube.com/oembed"))
            {
                if (uri.Contains("invalidKey"))
                {
                    return new HttpResponseMessage(System.Net.HttpStatusCode.NotFound);
                }
                return new HttpResponseMessage(System.Net.HttpStatusCode.OK);
            }
            return new HttpResponseMessage(System.Net.HttpStatusCode.NotFound);
        });

        var service = new TaquillaService(context, new HttpClient(fakeHandler));
        var result = await service.ObtenerTrailerPeliculaAsync(500);

        result.Should().NotBeNull();
        result!.IdPelicula.Should().Be(500);
        result.TrailerKey.Should().Be("kN3lE7rpggQ");
        result.Trailers.Should().HaveCount(2);
    }

    [Fact]
    public async Task ObtenerTrailerPeliculaAsync_WithSubtitleColon_FallsBackToBaseTitle()
    {
        using var context = GetInMemoryDbContext();
        var pelicula = new Pelicula
        {
            IdPelicula = 501,
            Titulo = "Super Mario: El Comienzo",
            Sinopsis = "Sinopsis de mario",
            Duracion = 90,
            Director = "Nintendo",
            Clasificacion = "AA"
        };
        context.Peliculas.Add(pelicula);
        await context.SaveChangesAsync();

        var emptyResultsJson = "{\"results\":[]}";
        var foundResultsJson = "{\"results\":[{\"id\":999,\"title\":\"Super Mario\"}]}";
        var videosJson = "{\"results\":[{\"site\":\"YouTube\",\"type\":\"Trailer\",\"key\":\"marioKey\",\"name\":\"Tráiler Oficial\"}]}";

        var fakeHandler = new FakeHttpMessageHandler((req) =>
        {
            var uri = req.RequestUri?.ToString() ?? "";
            if (uri.Contains("api.themoviedb.org/3/search/movie"))
            {
                if (uri.Contains("Super%20Mario%3A") || uri.Contains("Super%20Mario:"))
                {
                    return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
                    {
                        Content = new StringContent(emptyResultsJson, System.Text.Encoding.UTF8, "application/json")
                    };
                }
                return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
                {
                    Content = new StringContent(foundResultsJson, System.Text.Encoding.UTF8, "application/json")
                };
            }
            if (uri.Contains("api.themoviedb.org/3/movie/999/videos"))
            {
                return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
                {
                    Content = new StringContent(videosJson, System.Text.Encoding.UTF8, "application/json")
                };
            }
            if (uri.Contains("youtube.com/oembed"))
            {
                return new HttpResponseMessage(System.Net.HttpStatusCode.OK);
            }
            return new HttpResponseMessage(System.Net.HttpStatusCode.NotFound);
        });

        var service = new TaquillaService(context, new HttpClient(fakeHandler));
        var result = await service.ObtenerTrailerPeliculaAsync(501);

        result.Should().NotBeNull();
        result!.TrailerKey.Should().Be("marioKey");
    }

    [Fact]
    public async Task ObtenerTrailerPeliculaAsync_WhenTmdbReturnsNoVideos_FallsBackToPreconfiguredTrailers()
    {
        using var context = GetInMemoryDbContext();
        var pelicula = new Pelicula
        {
            IdPelicula = 1,
            Titulo = "Spider-Man: Un Nuevo Día",
            Sinopsis = "Sinopsis spiderman",
            Duracion = 120,
            Director = "Director Spiderman",
            Clasificacion = "B"
        };
        context.Peliculas.Add(pelicula);
        await context.SaveChangesAsync();

        var fakeHandler = new FakeHttpMessageHandler((req) =>
        {
            return new HttpResponseMessage(System.Net.HttpStatusCode.NotFound);
        });

        var service = new TaquillaService(context, new HttpClient(fakeHandler));
        var result = await service.ObtenerTrailerPeliculaAsync(1);

        result.Should().NotBeNull();
        result!.TrailerKey.Should().Be("QXibcL7-XbU");
    }

    [Fact]
    public async Task ObtenerTrailerPeliculaAsync_WhenCached_ReturnsCachedImmediately()
    {
        using var context = GetInMemoryDbContext();
        var pelicula = new Pelicula
        {
            IdPelicula = 14,
            Titulo = "Caminos del crimen",
            Sinopsis = "Sinopsis crimen",
            Duracion = 110,
            Director = "Director Crimen",
            Clasificacion = "B15"
        };
        context.Peliculas.Add(pelicula);
        await context.SaveChangesAsync();

        var fakeHandler = new FakeHttpMessageHandler((req) => throw new HttpRequestException("Network failure"));
        var service = new TaquillaService(context, new HttpClient(fakeHandler));

        var firstCall = await service.ObtenerTrailerPeliculaAsync(14);
        firstCall.Should().NotBeNull();
        firstCall!.TrailerKey.Should().Be("R4GEdGRQVBU");

        var secondCall = await service.ObtenerTrailerPeliculaAsync(14);
        secondCall.Should().BeSameAs(firstCall);
    }

    // =========================================================================
    // Pruebas Unitarias HU-07: POS Dulcería, Lector de Código de Barras y Stock Fraccionado
    // =========================================================================

    [Fact]
    public async Task SeedDulceriaAsync_SeedsCategoriesProductsAndFractionalInventoryIdempotently()
    {
        using var context = GetInMemoryDbContext();

        await DbSeeder.SeedDulceriaAsync(context);
        // Segunda ejecución para validar idempotencia
        await DbSeeder.SeedDulceriaAsync(context);

        var categorias = await context.Categoria.ToListAsync();
        categorias.Should().Contain(c => c.Nombre == "Snacks");
        categorias.Should().Contain(c => c.Nombre == "Bebidas");
        categorias.Should().Contain(c => c.Nombre == "Pizzas");

        var palomitas = await context.Productos.FirstOrDefaultAsync(p => p.CodigoBarras == "7501001");
        palomitas.Should().NotBeNull();
        palomitas!.Nombre.Should().Be("Palomitas Grandes");
        palomitas.PrecioVenta.Should().Be(50.00m);

        var stockFraccionado = await context.InventarioStockFraccionados.FirstOrDefaultAsync();
        stockFraccionado.Should().NotBeNull();
        stockFraccionado!.FactorFraccion.Should().Be(8);
        stockFraccionado.StockFraccionesActual.Should().Be(56);
    }

    [Fact]
    public async Task ObtenerCatalogoDulceriaPosAsync_ReturnsSnacksBebidasAndStockFraccionadoMonitor()
    {
        using var context = GetInMemoryDbContext();
        await DbSeeder.SeedDulceriaAsync(context);

        var service = new TaquillaService(context);
        var catalogo = await service.ObtenerCatalogoDulceriaPosAsync();

        catalogo.Should().NotBeNull();
        catalogo.StockFraccionado.Should().NotBeNull();
        catalogo.StockFraccionado!.StockRebanadasActual.Should().Be(56);
        catalogo.StockFraccionado.PizzasCompletasEquivalentes.Should().Be(7);
        catalogo.StockFraccionado.EtiquetaMonitor.Should().Be("Stock: 56 rebanadas (7 pizzas)");
        catalogo.StockFraccionado.ProductoRebanada.Should().NotBeNull();
        catalogo.StockFraccionado.ProductoRebanada!.Precio.Should().Be(35.00m);
        catalogo.StockFraccionado.ProductoPizzaCompleta.Should().NotBeNull();
        catalogo.StockFraccionado.ProductoPizzaCompleta!.Precio.Should().Be(180.00m);

        catalogo.SnacksYBebidas.Should().HaveCount(5);
        catalogo.SnacksYBebidas.Should().Contain(p => p.Sku == "7501001" && p.Nombre == "Palomitas Grandes");
        catalogo.SnacksYBebidas.Should().Contain(p => p.Sku == "7501003" && p.Nombre == "Nachos con Queso");
    }

    [Fact]
    public async Task BuscarProductoPorSkuAsync_WhenValidBarcodeScanned_ReturnsProducto()
    {
        using var context = GetInMemoryDbContext();
        await DbSeeder.SeedDulceriaAsync(context);
        var service = new TaquillaService(context);

        var prod = await service.BuscarProductoPorSkuAsync("7501001");

        prod.Should().NotBeNull();
        prod!.Sku.Should().Be("7501001");
        prod.Nombre.Should().Be("Palomitas Grandes");
        prod.Precio.Should().Be(50.00m);
    }

    [Fact]
    public async Task BuscarProductoPorSkuAsync_WhenInvalidOrNotFoundOrZeroStock_HandlesCorrectly()
    {
        using var context = GetInMemoryDbContext();
        await DbSeeder.SeedDulceriaAsync(context);

        var agotado = await context.Productos.FirstAsync(p => p.CodigoBarras == "7501004");
        agotado.StockActual = 0;
        await context.SaveChangesAsync();

        var service = new TaquillaService(context);

        Func<Task> emptyAct = async () => await service.BuscarProductoPorSkuAsync("   ");
        await emptyAct.Should().ThrowAsync<ArgumentException>();

        var noExiste = await service.BuscarProductoPorSkuAsync("9999999");
        noExiste.Should().BeNull();

        Func<Task> zeroStockAct = async () => await service.BuscarProductoPorSkuAsync("7501004");
        await zeroStockAct.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*inventario 0*");
    }

    [Fact]
    public async Task CalcularOrdenPosAsync_InheritsBoletosAndAddsScannedSkuAndFractionalSlice()
    {
        using var context = GetInMemoryDbContext();
        await DbSeeder.SeedDulceriaAsync(context);
        var service = new TaquillaService(context);

        // Paso 1 heredado (HU-06): 1 boleto de $50.00 + Criterio 1 (HU-07): Escaneo de SKU 7501001 ($50.00)
        var req1 = new CalcularOrdenRequestDto
        {
            ItemsActuales = new List<RegistrarVentaItemDto>
            {
                new RegistrarVentaItemDto
                {
                    Tipo = "boleto",
                    IdFuncion = 10,
                    Sku = "BOL-GEN",
                    Nombre = "Terrifier 3: Payaso Siniestro (12:30)",
                    Cantidad = 1,
                    Precio = 50.00m,
                    Asientos = new List<string> { "C1" }
                }
            },
            SkuAgregar = "7501001",
            CantidadAgregar = 1
        };

        var res1 = await service.CalcularOrdenPosAsync(req1);

        res1.SubtotalBoletos.Should().Be(50.00m);
        res1.SubtotalDulceria.Should().Be(50.00m);
        res1.TotalVenta.Should().Be(100.00m);
        res1.TotalArticulos.Should().Be(2);
        res1.ProductoAgregado.Should().NotBeNull();
        res1.ProductoAgregado!.Nombre.Should().Be("Palomitas Grandes");

        // Criterio 3 (HU-07): Seleccionar "Rebanada Individual" (SKU 7501006, $35.00) descuenta 1 rebanada de 56 -> 55
        var req2 = new CalcularOrdenRequestDto
        {
            ItemsActuales = res1.Items.Select(i => new RegistrarVentaItemDto
            {
                IdProducto = i.IdProducto,
                IdFuncion = i.IdFuncion,
                Sku = i.Sku,
                Nombre = i.Nombre,
                Tipo = i.Tipo,
                Cantidad = i.Cantidad,
                Precio = i.PrecioUnitario,
                Asientos = i.Asientos
            }).ToList(),
            SkuAgregar = "7501006",
            CantidadAgregar = 1
        };

        var res2 = await service.CalcularOrdenPosAsync(req2);

        res2.TotalVenta.Should().Be(135.00m); // 50 + 50 + 35
        res2.StockFraccionado.Should().NotBeNull();
        res2.StockFraccionado!.StockRebanadasActual.Should().Be(55);
        res2.StockFraccionado.PizzasCompletasEquivalentes.Should().Be(6);
        res2.StockFraccionado.EtiquetaMonitor.Should().Be("Stock: 55 rebanadas (6 pizzas)");
    }

    [Fact]
    public async Task CalcularOrdenPosAsync_WhenAddingWholePizza_DeductsEightSlicesProportional()
    {
        using var context = GetInMemoryDbContext();
        await DbSeeder.SeedDulceriaAsync(context);
        var service = new TaquillaService(context);

        var req = new CalcularOrdenRequestDto
        {
            ItemsActuales = new List<RegistrarVentaItemDto>(),
            SkuAgregar = "7501007", // Pizza Completa (8 Rebanadas)
            CantidadAgregar = 1
        };

        var res = await service.CalcularOrdenPosAsync(req);

        res.TotalVenta.Should().Be(180.00m);
        res.StockFraccionado.Should().NotBeNull();
        res.StockFraccionado!.StockRebanadasActual.Should().Be(48); // 56 - 8 = 48
        res.StockFraccionado.PizzasCompletasEquivalentes.Should().Be(6); // 48 / 8 = 6
        res.StockFraccionado.EtiquetaMonitor.Should().Be("Stock: 48 rebanadas (6 pizzas)");
    }

    [Fact]
    public async Task CalcularOrdenPosAsync_WhenFractionalOrNormalStockExceeded_ThrowsInvalidOperationException()
    {
        using var context = GetInMemoryDbContext();
        await DbSeeder.SeedDulceriaAsync(context);
        var service = new TaquillaService(context);

        // Intentar pedir 8 pizzas completas (64 rebanadas) cuando solo hay 56 rebanadas (7 pizzas)
        var reqExcesoPizza = new CalcularOrdenRequestDto
        {
            ItemsActuales = new List<RegistrarVentaItemDto>(),
            SkuAgregar = "7501007",
            CantidadAgregar = 8
        };

        Func<Task> actPizza = async () => await service.CalcularOrdenPosAsync(reqExcesoPizza);
        await actPizza.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Stock fraccionado insuficiente*");

        // Producto con stock 0 impide agregarse (Regla de Negocio 1)
        var nachos = await context.Productos.FirstAsync(p => p.CodigoBarras == "7501003");
        nachos.StockActual = 0;
        await context.SaveChangesAsync();

        var reqCeroStock = new CalcularOrdenRequestDto
        {
            ItemsActuales = new List<RegistrarVentaItemDto>(),
            SkuAgregar = "7501003",
            CantidadAgregar = 1
        };

        Func<Task> actCero = async () => await service.CalcularOrdenPosAsync(reqCeroStock);
        await actCero.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*inventario actual 0*");
    }

    [Fact]
    public async Task RegistrarVentaAsync_WithFractionalPizzaItems_UpdatesMasterAndSliceStockProportionally()
    {
        using var context = GetInMemoryDbContext();
        await DbSeeder.SeedDulceriaAsync(context);
        var service = new TaquillaService(context);

        // Vender 1 Rebanada Individual (descuenta 1 rebanada -> de 56 baja a 55)
        var ventaRebanada = new RegistrarVentaDto
        {
            IdVendedor = 1,
            MetodoPago = "Efectivo",
            Items = new List<RegistrarVentaItemDto>
            {
                new RegistrarVentaItemDto
                {
                    Tipo = "dulceria",
                    Sku = "7501006",
                    Nombre = "Rebanada Individual",
                    Cantidad = 1,
                    Precio = 35.00m
                }
            }
        };

        await service.RegistrarVentaAsync(ventaRebanada);

        var sfDespues1 = await context.InventarioStockFraccionados.FirstAsync();
        sfDespues1.StockFraccionesActual.Should().Be(55);

        var prodRebanada = await context.Productos.FirstAsync(p => p.CodigoBarras == "7501006");
        var prodPizza = await context.Productos.FirstAsync(p => p.CodigoBarras == "7501007");
        prodRebanada.StockActual.Should().Be(55);
        prodPizza.StockActual.Should().Be(6); // 55 / 8 = 6 pizzas completas disponibles

        // Vender 7 rebanadas adicionales (total 8 rebanadas descontadas = 1 pizza completa menos -> 48 rebanadas = 6 pizzas)
        var venta7Rebanadas = new RegistrarVentaDto
        {
            IdVendedor = 1,
            MetodoPago = "Efectivo",
            Items = new List<RegistrarVentaItemDto>
            {
                new RegistrarVentaItemDto
                {
                    Tipo = "dulceria",
                    Sku = "7501006",
                    Nombre = "Rebanada Individual",
                    Cantidad = 7,
                    Precio = 35.00m
                }
            }
        };

        await service.RegistrarVentaAsync(venta7Rebanadas);

        var sfDespues8 = await context.InventarioStockFraccionados.FirstAsync();
        sfDespues8.StockFraccionesActual.Should().Be(48);
        prodRebanada.StockActual.Should().Be(48);
        prodPizza.StockActual.Should().Be(6);

        // Vender 1 Pizza Completa (descuenta 8 rebanadas -> 40 rebanadas = 5 pizzas)
        var ventaPizzaCompleta = new RegistrarVentaDto
        {
            IdVendedor = 1,
            MetodoPago = "Efectivo",
            Items = new List<RegistrarVentaItemDto>
            {
                new RegistrarVentaItemDto
                {
                    Tipo = "dulceria",
                    Sku = "7501007",
                    Nombre = "Pizza Completa (8 Rebanadas)",
                    Cantidad = 1,
                    Precio = 180.00m
                }
            }
        };

        await service.RegistrarVentaAsync(ventaPizzaCompleta);

        var sfFinal = await context.InventarioStockFraccionados.FirstAsync();
        sfFinal.StockFraccionesActual.Should().Be(40);
        prodRebanada.StockActual.Should().Be(40);
        prodPizza.StockActual.Should().Be(5);
    }

    [Fact]
    public async Task RegistrarVentaAsync_WhenProductStockIsZero_ThrowsInvalidOperationException()
    {
        using var context = GetInMemoryDbContext();
        await DbSeeder.SeedDulceriaAsync(context);

        var prod = await context.Productos.FirstAsync(p => p.CodigoBarras == "7501001");
        prod.StockActual = 0;
        await context.SaveChangesAsync();

        var service = new TaquillaService(context);
        var dto = new RegistrarVentaDto
        {
            IdVendedor = 1,
            MetodoPago = "Efectivo",
            Items = new List<RegistrarVentaItemDto>
            {
                new RegistrarVentaItemDto
                {
                    Tipo = "dulceria",
                    IdProducto = prod.IdProducto,
                    Sku = "7501001",
                    Nombre = "Palomitas Grandes",
                    Cantidad = 1,
                    Precio = 50.00m
                }
            }
        };

        Func<Task> act = async () => await service.RegistrarVentaAsync(dto);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*stock actual: 0*");
    }

    [Fact]
    public async Task ValidarPreordenQrAsync_WhenQrExceeds12Days_ThrowsInvalidOperationException()
    {
        using var context = GetInMemoryDbContext();
        var cliente = new Cliente { IdCliente = 1, Nombre = "Carlos Ruiz", NumeroTelefono = "5512345678", Correo = "carlos@test.com" };
        var pelicula = new Pelicula { IdPelicula = 1, Titulo = "Venom", Sinopsis = "Sinopsis", Duracion = 110, Clasificacion = "B", Director = "Dir" };
        var funcion = new Funcion { IdFuncion = 10, IdPelicula = 1, IdSala = 1, FechaFuncion = DateTime.Today, HoraInicio = DateTime.Today.AddHours(17), Precio = 50m, IdPeliculaNavigation = pelicula };
        var preordenExpirada = new Preorden
        {
            IdPreorden = 1,
            IdCliente = 1,
            IdFuncion = 10,
            FechaCreacion = DateTime.Today.AddDays(-13),
            EstadoActivo = true,
            Vigencia = 12,
            CodigoQr = "PRE-XIL-EXP13",
            IdClienteNavigation = cliente,
            IdFuncionNavigation = funcion
        };

        context.Clientes.Add(cliente);
        context.Peliculas.Add(pelicula);
        context.Funcions.Add(funcion);
        context.Preordens.Add(preordenExpirada);
        await context.SaveChangesAsync();

        var service = new TaquillaService(context);

        Func<Task> act = async () => await service.ValidarPreordenQrAsync("PRE-XIL-EXP13");
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*12 días de vigencia*");
    }

    [Fact]
    public async Task ValidarPreordenQrAsync_WhenAlreadyRedeemed_ThrowsInvalidOperationException()
    {
        using var context = GetInMemoryDbContext();
        var cliente = new Cliente { IdCliente = 1, Nombre = "Laura Gómez", NumeroTelefono = "5512345678", Correo = "laura@test.com" };
        var pelicula = new Pelicula { IdPelicula = 1, Titulo = "Venom", Sinopsis = "Sinopsis", Duracion = 110, Clasificacion = "B", Director = "Dir" };
        var funcion = new Funcion { IdFuncion = 10, IdPelicula = 1, IdSala = 1, FechaFuncion = DateTime.Today, HoraInicio = DateTime.Today.AddHours(17), Precio = 50m, IdPeliculaNavigation = pelicula };
        var preordenCanjeada = new Preorden
        {
            IdPreorden = 2,
            IdCliente = 1,
            IdFuncion = 10,
            FechaCreacion = DateTime.Today.AddDays(-2),
            EstadoActivo = false,
            Vigencia = 12,
            CodigoQr = "PRE-XIL-CANJE01",
            IdClienteNavigation = cliente,
            IdFuncionNavigation = funcion
        };

        context.Clientes.Add(cliente);
        context.Peliculas.Add(pelicula);
        context.Funcions.Add(funcion);
        context.Preordens.Add(preordenCanjeada);
        await context.SaveChangesAsync();

        var service = new TaquillaService(context);

        Func<Task> act = async () => await service.ValidarPreordenQrAsync("PRE-XIL-CANJE01");
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*ya fue canjeado*");
    }

    [Fact]
    public async Task ObtenerPreordenesActivasAsync_ReturnsOnlyActiveAndWithin12Days()
    {
        using var context = GetInMemoryDbContext();
        var cliente = new Cliente { IdCliente = 1, Nombre = "Pedro", NumeroTelefono = "5512345678", Correo = "pedro@test.com" };
        var pelicula = new Pelicula { IdPelicula = 1, Titulo = "Gladiador II", Sinopsis = "Sinopsis", Duracion = 150, Clasificacion = "B15", Director = "Ridley" };
        var funcion = new Funcion { IdFuncion = 10, IdPelicula = 1, IdSala = 1, FechaFuncion = DateTime.Today, HoraInicio = DateTime.Today.AddHours(20), Precio = 50m, IdPeliculaNavigation = pelicula };

        context.Clientes.Add(cliente);
        context.Peliculas.Add(pelicula);
        context.Funcions.Add(funcion);
        context.Preordens.AddRange(
            new Preorden { IdPreorden = 1, IdCliente = 1, IdFuncion = 10, FechaCreacion = DateTime.Today.AddDays(-3), EstadoActivo = true, Vigencia = 12, CodigoQr = "PRE-XIL-VIGENTE", IdClienteNavigation = cliente, IdFuncionNavigation = funcion },
            new Preorden { IdPreorden = 2, IdCliente = 1, IdFuncion = 10, FechaCreacion = DateTime.Today.AddDays(-15), EstadoActivo = true, Vigencia = 12, CodigoQr = "PRE-XIL-VENCIDA", IdClienteNavigation = cliente, IdFuncionNavigation = funcion },
            new Preorden { IdPreorden = 3, IdCliente = 1, IdFuncion = 10, FechaCreacion = DateTime.Today.AddDays(-1), EstadoActivo = false, Vigencia = 12, CodigoQr = "PRE-XIL-USADA", IdClienteNavigation = cliente, IdFuncionNavigation = funcion }
        );
        context.DetallePreordenBoletos.Add(new DetallePreordenBoleto { IdDetalle = 1, IdPreorden = 1, Caantidad = 2, Subtotal = 100m });
        await context.SaveChangesAsync();

        var service = new TaquillaService(context);
        var activas = await service.ObtenerPreordenesActivasAsync();

        activas.Should().HaveCount(1);
        activas[0].Codigo.Should().Be("PRE-XIL-VIGENTE");
        activas[0].Expirada.Should().BeFalse();
        activas[0].Canjeada.Should().BeFalse();
    }

    [Fact]
    public async Task CalcularOrdenPosAsync_PreservesPreordenItemsAndAllowsAddingExtraProducts()
    {
        using var context = GetInMemoryDbContext();
        await DbSeeder.SeedDulceriaAsync(context);
        var service = new TaquillaService(context);

        var req = new CalcularOrdenRequestDto
        {
            ItemsActuales = new List<RegistrarVentaItemDto>
            {
                new RegistrarVentaItemDto
                {
                    Tipo = "dulceria",
                    IdProducto = 1,
                    Sku = "7501001",
                    Nombre = "Palomitas Grandes (Pre-Orden)",
                    Cantidad = 1,
                    Precio = 50m,
                    EsPreorden = true,
                    CodigoPreorden = "PRE-XIL-84920"
                }
            },
            SkuAgregar = "7501001", // Cliente pide unas Palomitas Grandes extra aparte de su pre-orden
            CantidadAgregar = 1
        };

        var res = await service.CalcularOrdenPosAsync(req);

        res.Items.Should().HaveCount(2, "la línea de pre-orden no debe fusionarse ni alterarse al agregar productos extra");
        res.Items.Count(i => i.EsPreorden).Should().Be(1);
        res.Items.Count(i => !i.EsPreorden).Should().Be(1);
        res.TotalVenta.Should().Be(100m);
    }

    [Fact]
    public async Task RegistrarVentaAsync_WithCashAndPreorden_CalculatesChangePersistsPagoAndMarksPreordenRedeemed()
    {
        using var context = GetInMemoryDbContext();
        await DbSeeder.SeedDulceriaAsync(context);

        var sala = new Sala { IdSala = 1, NombreSala = "Sala 1", CapacidadTotal = 48, TipoPantalla = "2D" };
        var pelicula = new Pelicula { IdPelicula = 1, Titulo = "Terrifier 3", Sinopsis = "Sinopsis", Duracion = 120, Clasificacion = "C", Director = "Damien" };
        var funcion = new Funcion { IdFuncion = 10, IdPelicula = 1, IdSala = 1, FechaFuncion = DateTime.Today, HoraInicio = DateTime.Today.AddHours(12).AddMinutes(30), Precio = 50m, IdSalaNavigation = sala, IdPeliculaNavigation = pelicula };
        var b1 = new Butaca { IdButaca = 1, IdSala = 1, Fila = "C", Numero = 1 };
        var preorden = new Preorden
        {
            IdPreorden = 50,
            IdFuncion = 10,
            FechaCreacion = DateTime.Today,
            EstadoActivo = true,
            Vigencia = 12,
            CodigoQr = "PRE-XIL-84920"
        };

        context.Salas.Add(sala);
        context.Peliculas.Add(pelicula);
        context.Funcions.Add(funcion);
        context.Butacas.Add(b1);
        context.Preordens.Add(preorden);
        await context.SaveChangesAsync();

        var service = new TaquillaService(context);
        var dto = new RegistrarVentaDto
        {
            IdVendedor = 1,
            MetodoPago = "Efectivo",
            MontoRecibido = 200m,
            CodigoPreorden = "PRE-XIL-84920",
            Items = new List<RegistrarVentaItemDto>
            {
                new RegistrarVentaItemDto
                {
                    Tipo = "boleto",
                    IdFuncion = 10,
                    Nombre = "Terrifier 3 (12:30)",
                    Cantidad = 1,
                    Precio = 50m,
                    Asientos = new List<string> { "C1" },
                    EsPreorden = true,
                    CodigoPreorden = "PRE-XIL-84920"
                },
                new RegistrarVentaItemDto
                {
                    Tipo = "dulceria",
                    Sku = "7501003",
                    Nombre = "Nachos con Queso",
                    Cantidad = 1,
                    Precio = 65m
                }
            }
        };

        var ticket = await service.RegistrarVentaAsync(dto);

        ticket.Should().NotBeNull();
        ticket.Total.Should().Be(115m);
        ticket.MontoRecibido.Should().Be(200m);
        ticket.Cambio.Should().Be(85m);
        ticket.RfcCine.Should().Be("CTX-260414-9X0");
        ticket.CodigoBarrasTicket.Should().StartWith("75010");
        ticket.CodigoPreordenCanjeada.Should().Be("PRE-XIL-84920");

        var preordenEnDb = await context.Preordens.FirstAsync(p => p.IdPreorden == 50);
        preordenEnDb.EstadoActivo.Should().BeFalse("la pre-orden debe quedar marcada como canjeada");

        var canjeEnDb = await context.CanjePreordens.FirstOrDefaultAsync(c => c.IdPreorden == 50);
        canjeEnDb.Should().NotBeNull();
        canjeEnDb!.CodigoQr.Should().Be("PRE-XIL-84920");

        var pagoEnDb = await context.PagoVentas.FirstOrDefaultAsync(pv => pv.IdVenta == ticket.IdVenta);
        pagoEnDb.Should().NotBeNull();
        pagoEnDb!.Cambio.Should().Be(85m);
    }

    [Fact]
    public async Task RegistrarVentaAsync_WhenInsufficientCash_ThrowsArgumentException()
    {
        using var context = GetInMemoryDbContext();
        await DbSeeder.SeedDulceriaAsync(context);
        var service = new TaquillaService(context);

        var dto = new RegistrarVentaDto
        {
            IdVendedor = 1,
            MetodoPago = "Efectivo",
            MontoRecibido = 40m,
            Items = new List<RegistrarVentaItemDto>
            {
                new RegistrarVentaItemDto
                {
                    Tipo = "dulceria",
                    Sku = "7501001",
                    Nombre = "Palomitas Grandes",
                    Cantidad = 1,
                    Precio = 50m
                }
            }
        };

        Func<Task> act = async () => await service.RegistrarVentaAsync(dto);
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*efectivo recibido*menor al monto total*");
    }

    [Fact]
    public async Task RegistrarVentaAsync_WithMercadoPagoVoucher_PersistsVoucherAndZeroChange()
    {
        using var context = GetInMemoryDbContext();
        await DbSeeder.SeedDulceriaAsync(context);
        var service = new TaquillaService(context);

        var dto = new RegistrarVentaDto
        {
            IdVendedor = 1,
            MetodoPago = "Tarjeta Mercado Pago",
            FolioAutorizacion = "23423",
            Items = new List<RegistrarVentaItemDto>
            {
                new RegistrarVentaItemDto
                {
                    Tipo = "dulceria",
                    Sku = "7501001",
                    Nombre = "Palomitas Grandes",
                    Cantidad = 1,
                    Precio = 50m
                }
            }
        };

        var ticket = await service.RegistrarVentaAsync(dto);

        ticket.Total.Should().Be(50m);
        ticket.Cambio.Should().Be(0m);
        ticket.FolioAutorizacion.Should().Be("23423");

        var pagoEnDb = await context.PagoVentas.FirstAsync(pv => pv.IdVenta == ticket.IdVenta);
        pagoEnDb.FolioAutorizacion.Should().Be("23423");
      
    private static IConfiguration BuildTwilioConfig(
        string? accountSid = "ACTESTACCOUNT001",
        string? authToken = "test_auth_token_value",
        string? fromNumber = "+15005550006",
        string? messagingServiceSid = null)
    {
        var values = new Dictionary<string, string?>();
        if (accountSid != null) values["Twilio:AccountSid"] = accountSid;
        if (authToken != null) values["Twilio:AuthToken"] = authToken;
        if (fromNumber != null) values["Twilio:FromNumber"] = fromNumber;
        if (messagingServiceSid != null) values["Twilio:MessagingServiceSid"] = messagingServiceSid;

        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }

    private static async Task SeedPreordenForPhoneAsync(CineDbContext context, string telefono, string folio, int idBase)
    {
        var sala = new Sala { IdSala = idBase, NombreSala = $"Sala {idBase}", CapacidadTotal = 48, TipoPantalla = "2D" };
        var pelicula = new Pelicula
        {
            IdPelicula = idBase,
            Titulo = $"Pelicula {folio}",
            Sinopsis = "Sinopsis",
            Duracion = 110,
            Director = "Dir",
            Clasificacion = "B"
        };
        var funcion = new Funcion
        {
            IdFuncion = idBase,
            IdPelicula = idBase,
            IdSala = idBase,
            FechaFuncion = DateTime.Today,
            HoraInicio = DateTime.Today.AddHours(19),
            Precio = 50m,
            IdPeliculaNavigation = pelicula,
            IdSalaNavigation = sala
        };
        var cliente = new Cliente
        {
            IdCliente = idBase,
            Nombre = "Cliente Recuperacion",
            NumeroTelefono = telefono,
            Correo = $"{telefono}@xilotzin.local"
        };
        var preorden = new Preorden
        {
            IdPreorden = idBase,
            CodigoQr = folio,
            IdFuncion = idBase,
            IdCliente = idBase,
            FechaCreacion = DateTime.Today,
            Vigencia = 12,
            EstadoActivo = true,
            IdClienteNavigation = cliente,
            IdFuncionNavigation = funcion
        };
        var boleto = new DetallePreordenBoleto
        {
            IdDetalle = idBase,
            IdPreorden = idBase,
            Caantidad = 2,
            Subtotal = 100m
        };
        var categoria = new Categorium { IdCategoria = idBase, Nombre = "Combos" };
        var producto = new Producto
        {
            IdProducto = idBase,
            CodigoBarras = $"SKU{idBase}",
            Nombre = "Combo Pareja",
            IdCategoria = idBase,
            PrecioVenta = 120m,
            Costo = 60m,
            StockActual = 10,
            StockMinimo = 2,
            Activo = true,
            IdCategoriaNavigation = categoria
        };
        var dulce = new DetallePreordenDulcerium
        {
            IdDetalle = idBase,
            IdPreorden = idBase,
            IdProducto = idBase,
            Cantidad = 1,
            IdProductoNavigation = producto
        };

        context.Salas.Add(sala);
        context.Peliculas.Add(pelicula);
        context.Funcions.Add(funcion);
        context.Clientes.Add(cliente);
        context.Preordens.Add(preorden);
        context.DetallePreordenBoletos.Add(boleto);
        context.Categoria.Add(categoria);
        context.Productos.Add(producto);
        context.DetallePreordenDulceria.Add(dulce);
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task SolicitarYVerificarCodigoRecuperacionAsync_EndToEndWithTwilioSms_ReturnsClientPreorders()
    {
        using var context = GetInMemoryDbContext();
        await SeedPreordenForPhoneAsync(context, "7731234567", "PRE-XIL-88001", 801);

        string? capturedBody = null;
        string? capturedUri = null;
        string? capturedAuth = null;

        var fakeHandler = new FakeHttpMessageHandler((req) =>
        {
            capturedUri = req.RequestUri?.ToString();
            capturedAuth = req.Headers.Authorization?.Scheme;
            capturedBody = req.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
            return new HttpResponseMessage(System.Net.HttpStatusCode.Created)
            {
                Content = new StringContent("{\"sid\":\"SMTEST001\",\"status\":\"queued\"}", System.Text.Encoding.UTF8, "application/json")
            };
        });

        var store = new InMemoryOtpCodeStore();
        var service = new TaquillaService(context, new HttpClient(fakeHandler), BuildTwilioConfig(), store);

        var solicitud = await service.SolicitarCodigoRecuperacionAsync(new SolicitarCodigoPreordenDto
        {
            Telefono = "+52 773 123 4567"
        });

        solicitud.Enviado.Should().BeTrue();
        solicitud.TelefonoEnmascarado.Should().Be("******4567");
        solicitud.ExpiraEnMinutos.Should().Be(10);
        capturedUri.Should().Be("https://api.twilio.com/2010-04-01/Accounts/ACTESTACCOUNT001/Messages.json");
        capturedAuth.Should().Be("Basic");
        capturedBody.Should().Contain("To=%2B527731234567");

        var decodedBody = Uri.UnescapeDataString(capturedBody!);
        var match = Regex.Match(decodedBody, @"\b(\d{6})\b");
        match.Success.Should().BeTrue();

        var tickets = await service.VerificarCodigoRecuperacionAsync(new VerificarCodigoPreordenDto
        {
            Telefono = "7731234567",
            Codigo = match.Groups[1].Value
        });

        tickets.Should().HaveCount(1);
        tickets[0].Codigo.Should().Be("PRE-XIL-88001");
        tickets[0].BoletosGeneral.Should().Be(2);
        tickets[0].Combos.Should().HaveCount(1);
        tickets[0].Total.Should().Be(220m);
    }

    [Fact]
    public async Task SolicitarCodigoRecuperacionAsync_WhenNoPreordersForPhone_ThrowsKeyNotFoundWithoutCallingTwilio()
    {
        using var context = GetInMemoryDbContext();
        bool calledTwilio = false;
        var fakeHandler = new FakeHttpMessageHandler((req) =>
        {
            calledTwilio = true;
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK);
        });

        var service = new TaquillaService(context, new HttpClient(fakeHandler), BuildTwilioConfig(), new InMemoryOtpCodeStore());

        Func<Task> act = async () => await service.SolicitarCodigoRecuperacionAsync(new SolicitarCodigoPreordenDto
        {
            Telefono = "7731234567"
        });

        await act.Should().ThrowAsync<KeyNotFoundException>();
        calledTwilio.Should().BeFalse();
    }

    [Fact]
    public async Task SolicitarCodigoRecuperacionAsync_WhenCalledTwiceInsideCooldown_ThrowsTwilioSmsException429()
    {
        using var context = GetInMemoryDbContext();
        await SeedPreordenForPhoneAsync(context, "5512345678", "PRE-XIL-88002", 802);

        var fakeHandler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(System.Net.HttpStatusCode.Created));
        var config = BuildTwilioConfig(fromNumber: null, messagingServiceSid: "MGTESTSERVICE001");
        var service = new TaquillaService(context, new HttpClient(fakeHandler), config, new InMemoryOtpCodeStore());

        await service.SolicitarCodigoRecuperacionAsync(new SolicitarCodigoPreordenDto { Telefono = "5512345678" });

        Func<Task> act = async () => await service.SolicitarCodigoRecuperacionAsync(new SolicitarCodigoPreordenDto { Telefono = "5512345678" });
        var ex = await act.Should().ThrowAsync<TwilioSmsException>();
        ex.Which.StatusCode.Should().Be(429);
    }

    [Theory]
    [InlineData(System.Net.HttpStatusCode.Unauthorized, "{}", 503)]
    [InlineData(System.Net.HttpStatusCode.BadRequest, "{\"code\":21608,\"message\":\"Unverified\"}", 400)]
    [InlineData(System.Net.HttpStatusCode.BadRequest, "{\"code\":21211,\"message\":\"Invalid To\"}", 400)]
    [InlineData(System.Net.HttpStatusCode.BadRequest, "{\"code\":21408,\"message\":\"Permission\"}", 503)]
    [InlineData(System.Net.HttpStatusCode.InternalServerError, "server error", 502)]
    public async Task SolicitarCodigoRecuperacionAsync_WhenTwilioFails_RemovesOtpAndThrowsExpectedStatus(
        System.Net.HttpStatusCode twilioStatus, string responseJson, int expectedStatus)
    {
        using var context = GetInMemoryDbContext();
        await SeedPreordenForPhoneAsync(context, "5512349999", "PRE-XIL-88003", 803);

        var fakeHandler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(twilioStatus)
        {
            Content = new StringContent(responseJson, System.Text.Encoding.UTF8, "application/json")
        });

        var store = new InMemoryOtpCodeStore();
        var service = new TaquillaService(context, new HttpClient(fakeHandler), BuildTwilioConfig(), store);

        Func<Task> act = async () => await service.SolicitarCodigoRecuperacionAsync(new SolicitarCodigoPreordenDto { Telefono = "5512349999" });
        var ex = await act.Should().ThrowAsync<TwilioSmsException>();
        ex.Which.StatusCode.Should().Be(expectedStatus);

        store.TryGetCooldownSeconds("5512349999", out _).Should().BeFalse();
    }

    [Fact]
    public async Task SolicitarCodigoRecuperacionAsync_WhenNotConfiguredOrNetworkError_ThrowsTwilioSmsException()
    {
        using var context = GetInMemoryDbContext();
        await SeedPreordenForPhoneAsync(context, "5588776655", "PRE-XIL-88004", 804);

        var unconfiguredService = new TaquillaService(context, null, BuildTwilioConfig(accountSid: ""), new InMemoryOtpCodeStore());
        var exUnconfigured = await Assert.ThrowsAsync<TwilioSmsException>(
            () => unconfiguredService.SolicitarCodigoRecuperacionAsync(new SolicitarCodigoPreordenDto { Telefono = "5588776655" }));
        exUnconfigured.StatusCode.Should().Be(503);

        var badSidService = new TaquillaService(context, null, BuildTwilioConfig(accountSid: "BAD/SID?"), new InMemoryOtpCodeStore());
        var exBadSid = await Assert.ThrowsAsync<TwilioSmsException>(
            () => badSidService.SolicitarCodigoRecuperacionAsync(new SolicitarCodigoPreordenDto { Telefono = "5588776655" }));
        exBadSid.StatusCode.Should().Be(503);

        var networkFailHandler = new FakeHttpMessageHandler(_ => throw new HttpRequestException("Network down"));
        var networkFailService = new TaquillaService(context, new HttpClient(networkFailHandler), BuildTwilioConfig(), new InMemoryOtpCodeStore());
        var exNetwork = await Assert.ThrowsAsync<TwilioSmsException>(
            () => networkFailService.SolicitarCodigoRecuperacionAsync(new SolicitarCodigoPreordenDto { Telefono = "5588776655" }));
        exNetwork.StatusCode.Should().Be(502);
    }

    [Fact]
    public async Task VerificarCodigoRecuperacionAsync_WithInvalidInputsOrWrongCode_ThrowsArgumentException()
    {
        using var context = GetInMemoryDbContext();
        var store = new InMemoryOtpCodeStore();
        store.SaveCode("7731234567", "112233", TimeSpan.FromMinutes(10), maxAttempts: 2);

        var service = new TaquillaService(context, null, BuildTwilioConfig(), store);

        await Assert.ThrowsAsync<ArgumentException>(() => service.SolicitarCodigoRecuperacionAsync(null!));
        await Assert.ThrowsAsync<ArgumentException>(() => service.VerificarCodigoRecuperacionAsync(null!));
        await Assert.ThrowsAsync<ArgumentException>(() => service.VerificarCodigoRecuperacionAsync(
            new VerificarCodigoPreordenDto { Telefono = "7731234567", Codigo = "12" }));

        var exWrong = await Assert.ThrowsAsync<ArgumentException>(() => service.VerificarCodigoRecuperacionAsync(
            new VerificarCodigoPreordenDto { Telefono = "7731234567", Codigo = "999999" }));
        exWrong.Message.Should().Contain("Te quedan 1 intento");

        var exLocked = await Assert.ThrowsAsync<ArgumentException>(() => service.VerificarCodigoRecuperacionAsync(
            new VerificarCodigoPreordenDto { Telefono = "7731234567", Codigo = "888888" }));
        exLocked.Message.Should().Contain("máximo de intentos");

        var exNotFound = await Assert.ThrowsAsync<ArgumentException>(() => service.VerificarCodigoRecuperacionAsync(
            new VerificarCodigoPreordenDto { Telefono = "7731234567", Codigo = "112233" }));
        exNotFound.Message.Should().Contain("expiró");

        store.SaveCode("7739998877", "445566", TimeSpan.FromMinutes(10));
        var emptyTickets = await service.VerificarCodigoRecuperacionAsync(
            new VerificarCodigoPreordenDto { Telefono = "7739998877", Codigo = "445566" });
        emptyTickets.Should().BeEmpty();

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
