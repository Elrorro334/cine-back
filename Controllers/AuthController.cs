using cine_back.Dtos;
using cine_back.Infrastructure;
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
    public async Task<ActionResult<NetworkCheckResponseDto>> CheckNetwork()
    {
        string clientIp = GetClientIpAddress();
        var result = await _authService.CheckNetworkAsync(clientIp);

        if (!result.IsAllowed)
        {
            return StatusCode(StatusCodes.Status403Forbidden, result);
        }

        return Ok(result);
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginRequestDto request)
    {
        string clientIp = GetClientIpAddress();
        var result = await _authService.LoginAsync(request, clientIp);

        if (!result.Success)
        {
            if (result.Message.Contains("red local"))
            {
                return StatusCode(StatusCodes.Status403Forbidden, result);
            }
            return BadRequest(result);
        }

        return Ok(result);
    }

    private string GetClientIpAddress()
    {
        if (Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
        {
            var ipList = forwardedFor.ToString().Split(',');
            if (ipList.Length > 0)
            {
                return ipList[0].Trim();
            }
        }

        return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
    }
}