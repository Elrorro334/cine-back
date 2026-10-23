using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cine.Application.Dtos;
using Cine.Application.Services;
using Cine.Domain.Entities;
using Cine.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace cine_back.Tests.Services;

public class AuthServiceTests
{
    private static CineDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<CineDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new CineDbContext(options);
    }

    private static IConfiguration CreateConfiguration(string? jwtKey = "SuperClaveSecretaMuyLargaParaPruebasUnitarias1234567890!")
    {
        var inMemorySettings = new Dictionary<string, string?>();
        if (jwtKey != null)
        {
            inMemorySettings["Jwt:Key"] = jwtKey;
        }
        inMemorySettings["Jwt:Issuer"] = "CineTeatroXilotzinApi";
        inMemorySettings["Jwt:Audience"] = "CineTeatroXilotzinPWA";

        return new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();
    }

    [Fact]
    public async Task CheckNetworkAsync_WhenIpIsAuthorized_ReturnsAllowedTrue()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        context.RedesAutorizadas.Add(new RedesAutorizada
        {
            IdRed = 1,
            DireccionIp = "192.168.1.100"
        });
        await context.SaveChangesAsync();

        var service = new AuthService(context, CreateConfiguration());

        // Act
        var result = await service.CheckNetworkAsync("192.168.1.100");

        // Assert
        result.Should().NotBeNull();
        result.IsAllowed.Should().BeTrue();
        result.IpDetected.Should().Be("192.168.1.100");
        result.Message.Should().Contain("Red Autorizada");
    }

    [Fact]
    public async Task CheckNetworkAsync_WhenIpIsNotAuthorized_ReturnsAllowedFalse()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = new AuthService(context, CreateConfiguration());

        // Act
        var result = await service.CheckNetworkAsync("200.50.10.5");

        // Assert
        result.Should().NotBeNull();
        result.IsAllowed.Should().BeFalse();
        result.IpDetected.Should().Be("200.50.10.5");
        result.Message.Should().Contain("BLOQUEO POR RESTRICCION DE RED");
    }

    [Fact]
    public async Task LoginAsync_WhenNoAdminExists_AndRescueCredentialsProvided_ReturnsAdminSuccess()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = new AuthService(context, CreateConfiguration());

        var request = new LoginRequestDto
        {
            NombreUsuario = "admin",
            Password = "admin123"
        };

        // Act
        var result = await service.LoginAsync(request, "192.168.1.50");

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Rol.Should().Be("Admin");
        result.IdVendedor.Should().Be(0);
        result.Nombre.Should().Be("Administrador Supremo");
    }

    [Fact]
    public async Task LoginAsync_WhenRealAdminExists_RescueCredentialsFail()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var adminRole = new Rol { IdRol = 1, NombreRol = "Admin", Descripcion = "Administrador" };
        context.Rols.Add(adminRole);
        context.Vendedors.Add(new Vendedor
        {
            IdVendedor = 1,
            Nombre = "AdminReal",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("RealPass123"),
            EstadoActivo = true,
            FechaCreacion = DateTime.UtcNow,
            IdRol = 1,
            IdRolNavigation = adminRole
        });
        await context.SaveChangesAsync();

        var service = new AuthService(context, CreateConfiguration());

        var request = new LoginRequestDto
        {
            NombreUsuario = "admin",
            Password = "admin123"
        };

        // Act
        var result = await service.LoginAsync(request, "192.168.1.50");

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Usuario o contraseña incorrectos.");
    }

    [Fact]
    public async Task LoginAsync_WhenUserNotFound_ReturnsFail()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = new AuthService(context, CreateConfiguration());

        var request = new LoginRequestDto
        {
            NombreUsuario = "Inexistente",
            Password = "Password123"
        };

        // Act
        var result = await service.LoginAsync(request, "192.168.1.50");

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Usuario o contraseña incorrectos.");
    }

    [Fact]
    public async Task LoginAsync_WhenUserIsInactive_ReturnsFail()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var rol = new Rol { IdRol = 2, NombreRol = "Vendedor", Descripcion = "Vendedor de boletos" };
        context.Rols.Add(rol);
        context.Vendedors.Add(new Vendedor
        {
            IdVendedor = 10,
            Nombre = "UsuarioInactivo",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123"),
            EstadoActivo = false,
            FechaCreacion = DateTime.UtcNow,
            IdRol = 2,
            IdRolNavigation = rol
        });
        await context.SaveChangesAsync();

        var service = new AuthService(context, CreateConfiguration());

        var request = new LoginRequestDto
        {
            NombreUsuario = "UsuarioInactivo",
            Password = "Password123"
        };

        // Act
        var result = await service.LoginAsync(request, "192.168.1.50");

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Usuario o contraseña incorrectos.");
    }

    [Fact]
    public async Task LoginAsync_WhenPasswordIsIncorrect_ReturnsFail()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var rol = new Rol { IdRol = 2, NombreRol = "Vendedor", Descripcion = "Vendedor de boletos" };
        context.Rols.Add(rol);
        context.Vendedors.Add(new Vendedor
        {
            IdVendedor = 2,
            Nombre = "Juan",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPassword"),
            EstadoActivo = true,
            FechaCreacion = DateTime.UtcNow,
            IdRol = 2,
            IdRolNavigation = rol
        });
        await context.SaveChangesAsync();

        var service = new AuthService(context, CreateConfiguration());

        var request = new LoginRequestDto
        {
            NombreUsuario = "Juan",
            Password = "WrongPassword"
        };

        // Act
        var result = await service.LoginAsync(request, "192.168.1.50");

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Usuario o contraseña incorrectos.");
    }

    [Fact]
    public async Task LoginAsync_WhenVendedorAndIpNotAuthorized_ReturnsForbiddenMessage()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var rol = new Rol { IdRol = 2, NombreRol = "Vendedor", Descripcion = "Vendedor de boletos" };
        context.Rols.Add(rol);
        context.Vendedors.Add(new Vendedor
        {
            IdVendedor = 3,
            Nombre = "Pedro",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Pass123"),
            EstadoActivo = true,
            FechaCreacion = DateTime.UtcNow,
            IdRol = 2,
            IdRolNavigation = rol
        });
        await context.SaveChangesAsync();

        var service = new AuthService(context, CreateConfiguration());

        var request = new LoginRequestDto
        {
            NombreUsuario = "Pedro",
            Password = "Pass123"
        };

        // Act
        var result = await service.LoginAsync(request, "189.200.10.5");

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("El rol Vendedor solo tiene autorización de operar");
    }

    [Fact]
    public async Task LoginAsync_WhenVendedorAndIpAuthorized_ReturnsSuccessWithJwtToken()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var rol = new Rol { IdRol = 2, NombreRol = "Vendedor", Descripcion = "Vendedor de boletos" };
        context.Rols.Add(rol);
        context.Vendedors.Add(new Vendedor
        {
            IdVendedor = 4,
            Nombre = "Raul",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Pass123"),
            EstadoActivo = true,
            FechaCreacion = DateTime.UtcNow,
            IdRol = 2,
            IdRolNavigation = rol
        });
        context.RedesAutorizadas.Add(new RedesAutorizada
        {
            IdRed = 1,
            DireccionIp = "192.168.1.20"
        });
        await context.SaveChangesAsync();

        var service = new AuthService(context, CreateConfiguration());

        var request = new LoginRequestDto
        {
            NombreUsuario = "Raul",
            Password = "Pass123"
        };

        // Act
        var result = await service.LoginAsync(request, "192.168.1.20");

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Token.Should().NotBeNullOrWhiteSpace();
        result.Rol.Should().Be("Vendedor");
        result.Nombre.Should().Be("Raul");
        result.IdVendedor.Should().Be(4);
    }

    [Fact]
    public async Task LoginAsync_WhenJwtKeyIsMissing_ThrowsInvalidOperationException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var rol = new Rol { IdRol = 2, NombreRol = "Vendedor", Descripcion = "Vendedor de boletos" };
        context.Rols.Add(rol);
        context.Vendedors.Add(new Vendedor
        {
            IdVendedor = 5,
            Nombre = "Maria",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Pass123"),
            EstadoActivo = true,
            FechaCreacion = DateTime.UtcNow,
            IdRol = 2,
            IdRolNavigation = rol
        });
        context.RedesAutorizadas.Add(new RedesAutorizada
        {
            IdRed = 2,
            DireccionIp = "192.168.1.25"
        });
        await context.SaveChangesAsync();

        var service = new AuthService(context, CreateConfiguration(jwtKey: ""));

        var request = new LoginRequestDto
        {
            NombreUsuario = "Maria",
            Password = "Pass123"
        };

        // Act
        var act = async () => await service.LoginAsync(request, "192.168.1.25");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*JWT*");
    }

    [Fact]
    public async Task LoginAsync_WhenLoopbackIp_ResolvesClientIpWithoutCrashing()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = new AuthService(context, CreateConfiguration());

        var request = new LoginRequestDto
        {
            NombreUsuario = "NonExistentUser",
            Password = "Pass"
        };

        // Act
        var result = await service.LoginAsync(request, "127.0.0.1");

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
    }
}
