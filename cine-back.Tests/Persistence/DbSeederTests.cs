using System;
using System.Linq;
using System.Threading.Tasks;
using Cine.Domain.Entities;
using Cine.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace cine_back.Tests.Persistence;

public class DbSeederTests
{
    private static CineDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<CineDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new CineDbContext(options);
    }

    [Fact]
    public async Task SeedSeguridadYTematicasAsync_CreatesInitialData_AndIsIdempotent()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        db.RedesAutorizadas.Add(new RedesAutorizada
        {
            IdRed = 1,
            DireccionIp = "192.168.1.50"
        });
        await db.SaveChangesAsync();

        // Act 1: Initial Seed
        await DbSeeder.SeedSeguridadYTematicasAsync(db);

        // Assert 1
        var sucursal = await db.Sucursales.FirstOrDefaultAsync();
        sucursal.Should().NotBeNull();
        sucursal!.IpDefecto.Should().Be("192.168.1.50");

        var turnos = await db.TurnosLaborales.ToListAsync();
        turnos.Should().HaveCount(2);

        var config = await db.ConfiguracionesSeguridad.FirstOrDefaultAsync();
        config.Should().NotBeNull();
        config!.SucursalId.Should().Be(sucursal.Id);

        var tematicas = await db.TematicasSitio.Include(t => t.PlantillasCss).ToListAsync();
        tematicas.Should().HaveCount(4);
        tematicas.Count(t => t.EsActiva).Should().Be(1);
        tematicas.First(t => t.EsActiva).ClaveIdentificadora.Should().Be("theme-classic");

        foreach (var t in tematicas)
        {
            t.PlantillasCss.Should().HaveCount(1);
            t.PlantillasCss.First().RutaArchivoCss.Should().NotBeNullOrWhiteSpace();
        }

        // Act 2: Second run (Idempotency check)
        await DbSeeder.SeedSeguridadYTematicasAsync(db);

        // Assert 2
        (await db.Sucursales.CountAsync()).Should().Be(1);
        (await db.TurnosLaborales.CountAsync()).Should().Be(2);
        (await db.ConfiguracionesSeguridad.CountAsync()).Should().Be(1);
        (await db.TematicasSitio.CountAsync()).Should().Be(4);
        (await db.PlantillasCss.CountAsync()).Should().Be(4);
    }

    [Fact]
    public async Task SeedButacasAsync_CreatesButacasForSalas_AndIsIdempotent()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        db.Salas.AddRange(
            new Sala { IdSala = 1, NombreSala = "Sala 1", CapacidadTotal = 48, TipoPantalla = "Tradicional" },
            new Sala { IdSala = 2, NombreSala = "Sala 2", CapacidadTotal = 40, TipoPantalla = "3D" }
        );
        await db.SaveChangesAsync();

        // Act 1
        await DbSeeder.SeedButacasAsync(db);

        // Assert 1: Sala 1 has 48 butacas, Sala 2 has 40 butacas
        var butacasSala1 = await db.Butacas.Where(b => b.IdSala == 1).ToListAsync();
        var butacasSala2 = await db.Butacas.Where(b => b.IdSala == 2).ToListAsync();
        butacasSala1.Should().HaveCount(48);
        butacasSala2.Should().HaveCount(40);

        // Act 2: Idempotency
        await DbSeeder.SeedButacasAsync(db);
        (await db.Butacas.CountAsync()).Should().Be(88);
    }

    [Fact]
    public async Task SeedDulceriaAsync_CreatesCategoriasAndProductos_AndIsIdempotent()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();

        // Act 1
        await DbSeeder.SeedDulceriaAsync(db);

        // Assert 1
        var categorias = await db.Categoria.ToListAsync();
        categorias.Should().HaveCount(4);

        var productos = await db.Productos.ToListAsync();
        productos.Should().HaveCount(8);

        var stockFracc = await db.InventarioStockFraccionados.FirstOrDefaultAsync();
        stockFracc.Should().NotBeNull();
        stockFracc!.FactorFraccion.Should().Be(8);

        // Act 2: Idempotency
        await DbSeeder.SeedDulceriaAsync(db);
        (await db.Categoria.CountAsync()).Should().Be(4);
        (await db.Productos.CountAsync()).Should().Be(8);
        (await db.InventarioStockFraccionados.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task SeedPreordenesDemoAsync_CreatesSalasPeliculasYPreordenes()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();

        // Act
        await DbSeeder.SeedPreordenesDemoAsync(db);

        // Assert
        var salas = await db.Salas.ToListAsync();
        salas.Should().NotBeEmpty();

        var peliculas = await db.Peliculas.ToListAsync();
        peliculas.Should().NotBeEmpty();

        var funciones = await db.Funcions.ToListAsync();
        funciones.Should().NotBeEmpty();

        var preordenes = await db.Preordens.ToListAsync();
        preordenes.Should().NotBeEmpty();

        // Idempotency
        await DbSeeder.SeedPreordenesDemoAsync(db);
        (await db.Preordens.CountAsync()).Should().Be(preordenes.Count);
    }
}
