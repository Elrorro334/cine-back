using Cine.Application.Dtos;
using Cine.Application.Helpers;
using Cine.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace cine_back.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpGet("check-network")]
    public async Task<ActionResult<NetworkCheckResponseDto>> CheckNetwork(
        [FromHeader(Name = "X-Forwarded-For")] string? forwardedFor = null)
    {
        string clientIp = IpAddressHelper.ExtractClientIp(
            forwardedFor,
            HttpContext.Connection.RemoteIpAddress);

        var result = await _authService.CheckNetworkAsync(clientIp);

        if (!result.IsAllowed)
        {
            return StatusCode(StatusCodes.Status403Forbidden, result);
        }

        return Ok(result);
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponseDto>> Login(
        [FromBody] LoginRequestDto request,
        [FromHeader(Name = "X-Forwarded-For")] string? forwardedFor = null)
    {
        string clientIp = IpAddressHelper.ExtractClientIp(
            forwardedFor,
            HttpContext.Connection.RemoteIpAddress);

        var result = await _authService.LoginAsync(request, clientIp);

        if (!result.Success)
        {
            if (EsBloqueoAcceso(result.Message))
            {
                return StatusCode(StatusCodes.Status403Forbidden, result);
            }
            return BadRequest(result);
        }

        return Ok(result);
    }

    private static bool EsBloqueoAcceso(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return false;
        }

        return message.Contains("red local", StringComparison.OrdinalIgnoreCase)
            || message.Contains("restricción de red", StringComparison.OrdinalIgnoreCase)
            || message.Contains("horario laboral", StringComparison.OrdinalIgnoreCase);
    }
}
