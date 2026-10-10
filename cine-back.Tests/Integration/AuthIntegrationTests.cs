using cine_back.Dtos;

using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;

namespace cine_back.Tests.Integration;

public class AuthIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public AuthIntegrationTests(WebApplicationFactory<Program> factory)
    {
        // 1. Forzamos el entorno "Development" para que el servidor de pruebas 
        // pueda leer tu conexión a SQL Server desde appsettings.Development.json
        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
        }).CreateClient();
    }

    [Fact]
    public async Task CheckNetwork_Endpoint_ReturnsExpectedResult()
    {
        // Act
        var response = await _client.GetAsync("/api/Auth/check-network");

        // Assert
        // 2. Aceptamos tanto 200 (OK) como 403 (Forbidden). 
        // Lo que nos importa es que la API no se caiga con un 500 o regrese un 404.
        response.StatusCode.Should().Match(
            status => status == HttpStatusCode.OK || status == HttpStatusCode.Forbidden,
            "el endpoint de seguridad debe procesar la solicitud y devolver 200 o 403"
        );

        var content = await response.Content.ReadFromJsonAsync<NetworkCheckResponseDto>();
        content.Should().NotBeNull();
        content!.IpDetected.Should().NotBeNullOrEmpty();
    }
}