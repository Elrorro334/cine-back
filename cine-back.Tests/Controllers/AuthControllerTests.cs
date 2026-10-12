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
        _authServiceMock = new Mock<IAuthService>();
        _controller = new AuthController(_authServiceMock.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }

    [Fact]
    public async Task CheckNetwork_WhenRedAutorizada_Returns200Ok()
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

    [Fact]
    public async Task CheckNetwork_WhenRedNoAutorizada_Returns403Forbidden()
    {
        // Arrange
        var mockResponse = new NetworkCheckResponseDto
        {
            IsAllowed = false,
            IpDetected = "187.190.22.40",
            Message = "BLOQUEO POR RESTRICCION DE RED (IP EXTERNA)."
        };

        _authServiceMock.Setup(s => s.CheckNetworkAsync(It.IsAny<string>()))
            .ReturnsAsync(mockResponse);

        // Act
        var result = await _controller.CheckNetwork();

        // Assert
        var objectResult = result.Result as ObjectResult;
        objectResult.Should().NotBeNull();
        objectResult!.StatusCode.Should().Be(403);
    }

    [Fact]
    public async Task Login_WhenCredentialsAndIpAreValid_Returns200OkWithToken()
    {
        // Arrange
        var request = new LoginRequestDto { NombreUsuario = "Raul", Password = "123" };
        var mockResponse = new LoginResponseDto
        {
            Success = true,
            Message = "Inicio de sesión exitoso.",
            IdVendedor = 1002,
            Nombre = "Raul",
            Rol = "Vendedor",
            Token = "eyJhbGciOiJIUzI1Ni..."
        };

        _authServiceMock.Setup(s => s.LoginAsync(It.IsAny<LoginRequestDto>(), It.IsAny<string>()))
            .ReturnsAsync(mockResponse);

        // Act
        var result = await _controller.Login(request);

        // Assert
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);

        var responseDto = okResult.Value as LoginResponseDto;
        responseDto.Should().NotBeNull();
        responseDto!.Token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_WhenCredentialsAreInvalid_Returns400BadRequest()
    {
        // Arrange
        var request = new LoginRequestDto { NombreUsuario = "Raul", Password = "wrongpassword" };
        var mockResponse = new LoginResponseDto
        {
            Success = false,
            Message = "Usuario o contraseña incorrectos."
        };

        _authServiceMock.Setup(s => s.LoginAsync(It.IsAny<LoginRequestDto>(), It.IsAny<string>()))
            .ReturnsAsync(mockResponse);

        // Act
        var result = await _controller.Login(request);

        // Assert
        var badRequestResult = result.Result as BadRequestObjectResult;
        badRequestResult.Should().NotBeNull();
        badRequestResult!.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Login_WhenVendedorIpIsBlocked_Returns403Forbidden()
    {
        // Arrange
        var request = new LoginRequestDto { NombreUsuario = "Raul", Password = "123" };
        var mockResponse = new LoginResponseDto
        {
            Success = false,
            Message = "El rol Vendedor solo tiene autorización de operar conectado a la red local del cine."
        };

        _authServiceMock.Setup(s => s.LoginAsync(It.IsAny<LoginRequestDto>(), It.IsAny<string>()))
            .ReturnsAsync(mockResponse);

        // Act
        var result = await _controller.Login(request);

        // Assert
        var forbiddenResult = result.Result as ObjectResult;
        forbiddenResult.Should().NotBeNull();
        forbiddenResult!.StatusCode.Should().Be(403);
    }
}