using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Cine.Application.Dtos;
using Cine.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace cine_back.Tests.Integration;

public class AuthIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string TestJwtKey = "ClaveSinteticaMuyLargaYSeguraParaTestExplorerLocal1234567890";
    private const string TestIssuer = "CineTeatroXilotzinApi";
    private const string TestAudience = "CineTeatroXilotzinPWA";

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
                    { "Jwt:Key", TestJwtKey },
                    { "Jwt:Issuer", TestIssuer },
                    { "Jwt:Audience", TestAudience }
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
                    options.UseInMemoryDatabase("TestCineDb_Integration")
                           .UseInternalServiceProvider(serviceProvider);
                });
            });
        });
    }

    private static string GenerarJwtValido()
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestJwtKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "1"),
            new Claim(ClaimTypes.Name, "Raul"),
            new Claim(ClaimTypes.Role, "Vendedor")
        };

        var token = new JwtSecurityToken(
            issuer: TestIssuer,
            audience: TestAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    [Fact]
    public async Task CheckNetwork_Endpoint_ReturnExpectedResult()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/auth/check-network");

        response.StatusCode.Should().Match(status =>
            status == HttpStatusCode.OK || status == HttpStatusCode.Forbidden,
            $"El endpoint devolvió {response.StatusCode}");
    }

    // =========================================================================
    // PRUEBAS DE SEGURIDAD (HU-07): Bloqueo sin JWT y con JWT apócrifo
    // =========================================================================

    [Theory]
    [InlineData("/api/taquilla/dulceria")]
    [InlineData("/api/taquilla/dulceria/catalogo-pos")]
    [InlineData("/api/taquilla/dulceria/sku/7501001")]
    public async Task DulceriaPosGetEndpoints_WithoutJwtToken_ReturnsUnauthorized(string endpoint)
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync(endpoint);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CalcularOrdenPos_WithoutJwtToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/taquilla/dulceria/calcular-orden", new
        {
            itemsActuales = Array.Empty<object>(),
            skuAgregar = "7501001",
            cantidadAgregar = 1
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DulceriaPos_WithForgedJwtToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "token.falso.invalido");

        var response = await client.GetAsync("/api/taquilla/dulceria/catalogo-pos");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // =========================================================================
    // PRUEBAS DE INTEGRACIÓN END-TO-END CON JWT VÁLIDO (HU-07)
    // =========================================================================

    [Fact]
    public async Task FlujoIntegracionDulceriaPos_ConJwtValido_RetornaCatalogoEscanerYCalculoDeOrden()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CineDbContext>();
            await DbSeeder.SeedDulceriaAsync(db);
        }

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", GenerarJwtValido());

        // 1. Consultar catálogo POS con monitor de pizzas (56 rebanadas / 7 pizzas)
        var catalogoResp = await client.GetAsync("/api/taquilla/dulceria/catalogo-pos");
        catalogoResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var catalogo = await catalogoResp.Content.ReadFromJsonAsync<CatalogoDulceriaPosDto>();
        catalogo.Should().NotBeNull();
        catalogo!.StockFraccionado.Should().NotBeNull();
        catalogo.StockFraccionado!.EtiquetaMonitor.Should().Be("Stock: 56 rebanadas (7 pizzas)");

        // 2. Escanear SKU 7501001 (Palomitas Grandes - $50.00)
        var skuResp = await client.GetAsync("/api/taquilla/dulceria/sku/7501001");
        skuResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var palomitas = await skuResp.Content.ReadFromJsonAsync<ProductoDto>();
        palomitas.Should().NotBeNull();
        palomitas!.Nombre.Should().Be("Palomitas Grandes");
        palomitas.Precio.Should().Be(50.00m);

        // 3. Calcular orden agregando 1 Rebanada Individual (SKU 7501006 - $35.00)
        var calcResp = await client.PostAsJsonAsync("/api/taquilla/dulceria/calcular-orden", new CalcularOrdenRequestDto
        {
            ItemsActuales = new List<RegistrarVentaItemDto>(),
            SkuAgregar = "7501006",
            CantidadAgregar = 1
        });
        calcResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var resumen = await calcResp.Content.ReadFromJsonAsync<ResumenOrdenCalculadoDto>();
        resumen.Should().NotBeNull();
        resumen!.TotalVenta.Should().Be(35.00m);
        resumen.StockFraccionado!.StockRebanadasActual.Should().Be(55);
        resumen.StockFraccionado.EtiquetaMonitor.Should().Be("Stock: 55 rebanadas (6 pizzas)");
    }
}
