using cine_back.Data;
using cine_back.Models;
using cine_back.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace cine_back.Tests.Services;

public class TaquillaServiceTests
{
    private CineDbContext GetInMemoryDbContext()
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
}