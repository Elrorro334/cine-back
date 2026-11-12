using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cine.Application.Dtos;
using Cine.Application.Exceptions;
using Cine.Application.Interfaces;
using Cine.Application.Services;
using Cine.Domain.Entities;
using Cine.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace cine_back.Tests.Services;

public class ConfiguracionAdminServiceTests
{
    private static CineDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<CineDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new CineDbContext(options);
    }

    private static Mock<ICineClock> CreateClockMock(DateTime? now = null)
    {
        var fecha = now ?? new DateTime(2026, 11, 11, 14, 0, 0);
        var mock = new Mock<ICineClock>();
        mock.Setup(c => c.GetLocalNow()).Returns(fecha);
        mock.Setup(c => c.GetLocalTimeOfDay()).Returns(fecha.TimeOfDay);
        return mock;
    }

    [Fact]
    public async Task ObtenerConfiguracionSeguridadAsync_WhenNoConfigExists_ReturnsDefaults()
    {
        using var db = CreateInMemoryDbContext();
        var clock = CreateClockMock();
        var broadcaster = new TematicaEventBroadcaster();
        var service = new ConfiguracionAdminService(db, clock.Object, broadcaster);

        var result = await service.ObtenerConfiguracionSeguridadAsync("192.168.1.50");

        result.Should().NotBeNull();
        result.IpAutorizada.Should().Be("192.168.1.100");
        result.HorarioInicioCajeros.Should().Be("08:00:00");
        result.HorarioFinCajeros.Should().Be("23:00:00");
        result.AutorizarExcepcionHorasExtras.Should().BeFalse();
        result.IpClienteDetectada.Should().Be("192.168.1.50");
        result.MismaSubredQueCliente.Should().BeTrue();
    }

    [Fact]
    public async Task ObtenerConfiguracionSeguridadAsync_WhenSeeded_ReturnsConfigAndTurnos()
    {
        using var db = CreateInMemoryDbContext();
        await DbSeeder.SeedSeguridadYTematicasAsync(db);
        var clock = CreateClockMock();
        var broadcaster = new TematicaEventBroadcaster();
        var service = new ConfiguracionAdminService(db, clock.Object, broadcaster);

        var result = await service.ObtenerConfiguracionSeguridadAsync("192.168.1.25");

        result.SucursalNombre.Should().Contain("Xilotzin");
        result.TurnosLaborales.Should().HaveCount(2);
        result.TurnosLaborales.Select(t => t.NombreTurno).Should().Contain(new[] { "Matutino", "Vespertino" });
    }

    [Fact]
    public async Task ActualizarConfiguracionSeguridadAsync_SameSubnet_UpdatesConfigAndSyncsRedes()
    {
        using var db = CreateInMemoryDbContext();
        await DbSeeder.SeedSeguridadYTematicasAsync(db);
        var clock = CreateClockMock();
        var broadcaster = new TematicaEventBroadcaster();
        var service = new ConfiguracionAdminService(db, clock.Object, broadcaster);

        var dto = new ActualizarConfiguracionSeguridadDto
        {
            IpAutorizada = "192.168.1.200",
            HorarioInicioCajeros = "08:00",
            HorarioFinCajeros = "23:00",
            AutorizarExcepcionHorasExtras = true,
            ConfirmarCambioRed = false
        };

        var result = await service.ActualizarConfiguracionSeguridadAsync(dto, "192.168.1.10", 5);

        result.IpAutorizada.Should().Be("192.168.1.200");
        result.HorarioInicioCajeros.Should().Be("08:00:00");
        result.HorarioFinCajeros.Should().Be("23:00:00");
        result.AutorizarExcepcionHorasExtras.Should().BeTrue();
        result.UsuarioModificoId.Should().Be(5);
        result.MismaSubredQueCliente.Should().BeTrue();

        var persisted = await db.ConfiguracionesSeguridad.FirstAsync();
        persisted.IpAutorizada.Should().Be("192.168.1.200");
        persisted.AutorizarExcepcionHorasExtras.Should().BeTrue();

        var redSincronizada = await db.RedesAutorizadas.AnyAsync(r => r.DireccionIp == "192.168.1.200");
        redSincronizada.Should().BeTrue();
    }

    [Fact]
    public async Task ActualizarConfiguracionSeguridadAsync_DifferentSubnetWithoutConfirmation_ThrowsSubredDiferenteException()
    {
        using var db = CreateInMemoryDbContext();
        await DbSeeder.SeedSeguridadYTematicasAsync(db);
        var clock = CreateClockMock();
        var broadcaster = new TematicaEventBroadcaster();
        var service = new ConfiguracionAdminService(db, clock.Object, broadcaster);

        var dto = new ActualizarConfiguracionSeguridadDto
        {
            IpAutorizada = "10.20.30.40",
            HorarioInicioCajeros = "08:00:00",
            HorarioFinCajeros = "23:00:00",
            AutorizarExcepcionHorasExtras = false,
            ConfirmarCambioRed = false
        };

        var act = async () => await service.ActualizarConfiguracionSeguridadAsync(dto, "192.168.1.10", 1);

        var ex = await act.Should().ThrowAsync<SubredDiferenteException>();
        ex.Which.IpClienteDetectada.Should().Be("192.168.1.10");
        ex.Which.IpSolicitada.Should().Be("10.20.30.40");
    }

    [Fact]
    public async Task ActualizarConfiguracionSeguridadAsync_DifferentSubnetWithConfirmation_UpdatesSuccessfully()
    {
        using var db = CreateInMemoryDbContext();
        var clock = CreateClockMock();
        var broadcaster = new TematicaEventBroadcaster();
        var service = new ConfiguracionAdminService(db, clock.Object, broadcaster);

        var dto = new ActualizarConfiguracionSeguridadDto
        {
            IpAutorizada = "2001:db8:85a3::8a2e:370:7334",
            HorarioInicioCajeros = "09:00:00",
            HorarioFinCajeros = "22:30:00",
            AutorizarExcepcionHorasExtras = false,
            ConfirmarCambioRed = true
        };

        var result = await service.ActualizarConfiguracionSeguridadAsync(dto, "192.168.1.10", 2);

        result.IpAutorizada.Should().Be("2001:db8:85a3::8a2e:370:7334");
        result.MismaSubredQueCliente.Should().BeFalse();
    }

    [Theory]
    [InlineData("999.999.1.1", "08:00", "23:00")]
    [InlineData("192.168.1.200", "25:00", "23:00")]
    [InlineData("192.168.1.200", "08:00", "08:00")]
    public async Task ActualizarConfiguracionSeguridadAsync_InvalidInputs_ThrowsArgumentException(
        string ip,
        string inicio,
        string fin)
    {
        using var db = CreateInMemoryDbContext();
        var service = new ConfiguracionAdminService(db, CreateClockMock().Object, new TematicaEventBroadcaster());

        var dto = new ActualizarConfiguracionSeguridadDto
        {
            IpAutorizada = ip,
            HorarioInicioCajeros = inicio,
            HorarioFinCajeros = fin,
            ConfirmarCambioRed = true
        };

        var act = async () => await service.ActualizarConfiguracionSeguridadAsync(dto, "192.168.1.10", 1);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Tematicas_SeedAndActivation_EnforcesSingleActiveAndBroadcastsEvent()
    {
        using var db = CreateInMemoryDbContext();
        await DbSeeder.SeedSeguridadYTematicasAsync(db);

        var clock = CreateClockMock(new DateTime(2026, 10, 31, 18, 0, 0));
        var broadcaster = new TematicaEventBroadcaster();
        var (subId, reader) = broadcaster.Subscribe();

        try
        {
            var service = new ConfiguracionAdminService(db, clock.Object, broadcaster);

            var todas = await service.ObtenerTematicasAsync();
            todas.Should().HaveCount(4);
            todas.Count(t => t.EsActiva).Should().Be(1);

            var activaInicial = await service.ObtenerTematicaActivaAsync();
            activaInicial.Should().NotBeNull();
            activaInicial!.ClaveIdentificadora.Should().Be("theme-classic");

            var halloween = todas.First(t => t.ClaveIdentificadora == "theme-halloween");
            var activada = await service.ActivarTematicaAsync(halloween.Id);

            activada.EsActiva.Should().BeTrue();
            activada.RutaArchivoCss.Should().Be("/css/themes/halloween.min.css");
            activada.Icono.Should().Be("fa-ghost");

            var estadoBd = await db.TematicasSitio.ToListAsync();
            estadoBd.Count(t => t.EsActiva).Should().Be(1);
            estadoBd.Single(t => t.EsActiva).Id.Should().Be(halloween.Id);

            reader.TryRead(out var broadcasted).Should().BeTrue();
            broadcasted.Should().NotBeNull();
            broadcasted!.RutaArchivoCss.Should().Be("/css/themes/halloween.min.css");
        }
        finally
        {
            broadcaster.Unsubscribe(subId);
        }
    }

    [Fact]
    public async Task ActivarTematicaAsync_WhenNotFound_ThrowsKeyNotFoundException()
    {
        using var db = CreateInMemoryDbContext();
        var service = new ConfiguracionAdminService(db, CreateClockMock().Object, new TematicaEventBroadcaster());

        var act = async () => await service.ActivarTematicaAsync(999);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }
}
