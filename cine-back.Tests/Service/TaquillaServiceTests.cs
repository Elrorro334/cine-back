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
        // Arrange
        using var context = GetInMemoryDbContext();

        // 1. Llenamos la Sala con sus propiedades obligatorias
        var sala = new Sala
        {
            IdSala = 1,
            NombreSala = "Sala 1",
            CapacidadTotal = 100,
            TipoPantalla = "MacroXE"
        };
        context.Salas.Add(sala);

        // 2. Llenamos las Películas con sus propiedades obligatorias
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

        // 3. Agregamos las Funciones (una de hoy y una de ayer)
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

        // Act
        var result = await service.ObtenerCarteleraHoyAsync(null);

        // Assert
        result.Should().HaveCount(1);
        result[0].Titulo.Should().Be("Pelicula De Hoy");
        result[0].Funciones.Should().HaveCount(1);
    }

    [Fact]
    public async Task RegistrarPeliculaDesdeTmdbAsync_StoresOnlyRelativePosterPathAndTruncatesSynopsis()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var service = new TaquillaService(context);

        var dto = new Cine.Application.Dtos.RegistrarPeliculaDto
        {
            Titulo = "Batman 2026",
            Sinopsis = new string('A', 150), // Más de 100 caracteres
            Duracion = 140,
            Clasificacion = "B",
            Director = "Matt Reeves",
            PosterPath = "https://image.tmdb.org/t/p/w500/batman2026.jpg", // URL absoluta que debe sanitizarse
            Precio = 65.00m,
            Horario = "17:30"
        };

        // Act
        var result = await service.RegistrarPeliculaDesdeTmdbAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.Titulo.Should().Be("Batman 2026");
        result.poster_image.Should().Be("/batman2026.jpg"); // Solo ruta relativa
        result.poster_image.Should().NotContain("http");
        result.Sinopsis.Length.Should().BeLessThanOrEqualTo(100);
        result.Sinopsis.Should().EndWith("...");

        var peliculaEnDb = await context.Peliculas.FirstAsync(p => p.IdPelicula == result.IdPelicula);
        peliculaEnDb.PosterImage.Should().Be("/batman2026.jpg");
        peliculaEnDb.Sinopsis.Length.Should().BeLessThanOrEqualTo(100);
    }

    [Fact]
    public async Task RegistrarPeliculaDesdeTmdbAsync_WhenTitleAlreadyExists_ReusesPeliculaWithoutDuplicating()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var service = new TaquillaService(context);

        var dto1 = new Cine.Application.Dtos.RegistrarPeliculaDto
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

        var dto2 = new Cine.Application.Dtos.RegistrarPeliculaDto
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

        // Act
        var res1 = await service.RegistrarPeliculaDesdeTmdbAsync(dto1);
        var res2 = await service.RegistrarPeliculaDesdeTmdbAsync(dto2);

        // Assert
        res1.IdPelicula.Should().Be(res2.IdPelicula); // Reutiliza el mismo ID
        var peliculasCount = await context.Peliculas.CountAsync(p => p.Titulo == "Slender Man");
        peliculasCount.Should().Be(1); // Cero duplicados
    }

    [Fact]
    public async Task EliminarPeliculaAsync_RemovesPeliculaAndCascadeFunctions()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var service = new TaquillaService(context);

        var dto = new Cine.Application.Dtos.RegistrarPeliculaDto
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

        // Act
        var eliminada = await service.EliminarPeliculaAsync(creada.IdPelicula);

        // Assert
        eliminada.Should().BeTrue();
        var existe = await context.Peliculas.AnyAsync(p => p.IdPelicula == creada.IdPelicula);
        existe.Should().BeFalse();
        var funcionesExisten = await context.Funcions.AnyAsync(f => f.IdPelicula == creada.IdPelicula);
        funcionesExisten.Should().BeFalse();
    }
}