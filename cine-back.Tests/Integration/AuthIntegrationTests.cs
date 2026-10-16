using System.Linq;
using System.Net;
using Cine.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace cine_back.Tests.Integration;

public class AuthIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public AuthIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "Jwt:Key", "ClaveSinteticaMuyLargaYSeguraParaTestExplorerLocal1234567890" },
                    { "Jwt:Issuer", "CineTeatroXilotzinApi" },
                    { "Jwt:Audience", "CineTeatroXilotzinPWA" }
                });
            });

            builder.ConfigureServices(services =>
            {
                // 1. Quitar la configuración principal que metió SQL Server
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<CineDbContext>));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                // 2. Crear un contenedor limpio y aislado EXCLUSIVO para InMemory
                var serviceProvider = new ServiceCollection()
                    .AddEntityFrameworkInMemoryDatabase()
                    .BuildServiceProvider();

                // 3. Inyectar la DB en memoria forzándola a usar su propio contenedor
                services.AddDbContext<CineDbContext>(options =>
                {
                    options.UseInMemoryDatabase("TestCineDb_" + Guid.NewGuid())
                           .UseInternalServiceProvider(serviceProvider);
                });
            });
        });
    }

    [Fact]
    public async Task CheckNetwork_Endpoint_ReturnExpectedResult()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/auth/check-network");

        // Assert
        response.StatusCode.Should().Match(status =>
            status == HttpStatusCode.OK || status == HttpStatusCode.Forbidden,
            $"El endpoint devolvió {response.StatusCode}");
    }
}