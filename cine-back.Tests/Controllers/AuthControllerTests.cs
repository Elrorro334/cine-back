using cine_back.Controllers;
using cine_back.Dtos;
using cine_back.Infrastructure;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace cine_back.Tests.Controllers;

public class AuthControllerTests
{
    private readonly Mock<IAuthService> _authServiceMock;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        // 1. Arrange: Configuramos el entorno y el Mock
        _authServiceMock = new Mock<IAuthService>();
        _controller = new AuthController(_authServiceMock.Object);

        // Simulamos el HttpContext para que Request.Headers no truene
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    [Fact]
    public async Task CheckNetwork_WhenRedAutorizada_ReturnsOk()
    {
        // Arrange
        var mockResponse = new NetworkCheckResponseDto
        {
            IsAllowed = true,
            IpDetected = "192.168.1.105",
            Message = "Red Autorizada"
        };

        _authServiceMock.Setup(s => s.CheckNetworkAsync(It.IsAny<string>()))
            .ReturnsAsync(mockResponse);

        // Act
        var result = await _controller.CheckNetwork();

        // Assert
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
        okResult.Value.Should().BeEquivalentTo(mockResponse);
    }
}