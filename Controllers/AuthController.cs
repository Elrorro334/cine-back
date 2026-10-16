using Cine.Application.Dtos;
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
        string clientIp = ExtractClientIp(forwardedFor);

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
        string clientIp = ExtractClientIp(forwardedFor);
        var result = await _authService.LoginAsync(request, clientIp);

        if (!result.Success)
        {
            if (result.Message.Contains("red local", StringComparison.OrdinalIgnoreCase))
            {
                return StatusCode(StatusCodes.Status403Forbidden, result);
            }
            return BadRequest(result);
        }

        return Ok(result);
    }

    private string ExtractClientIp(string? forwardedFor)
    {
        if (!string.IsNullOrWhiteSpace(forwardedFor))
        {
            var ipList = forwardedFor.Split(',');
            if (ipList.Length > 0 && !string.IsNullOrWhiteSpace(ipList[0]))
            {
                return ipList[0].Trim();
            }
        }

        return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
    }
}