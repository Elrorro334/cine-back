using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Cine.Application.Dtos;
using Cine.Application.Exceptions;
using Cine.Application.Helpers;
using Cine.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace cine_back.Controllers;

[Authorize(Roles = "Administrador,Admin")]
[ApiController]
[Route("api/[controller]")]
public class ConfiguracionController : ControllerBase
{
    private readonly IConfiguracionAdminService _configuracionService;

    public ConfiguracionController(IConfiguracionAdminService configuracionService)
    {
        _configuracionService = configuracionService;
    }

    [HttpGet("seguridad")]
    public async Task<ActionResult<ConfiguracionSeguridadDto>> GetSeguridad(
        [FromHeader(Name = "X-Forwarded-For")] string? forwardedFor = null)
    {
        string clientIp = IpAddressHelper.ExtractClientIp(
            forwardedFor,
            HttpContext.Connection.RemoteIpAddress);

        var config = await _configuracionService.ObtenerConfiguracionSeguridadAsync(clientIp);
        return Ok(config);
    }

    [HttpPut("seguridad")]
    public async Task<ActionResult<ConfiguracionSeguridadDto>> ActualizarSeguridad(
        [FromBody] ActualizarConfiguracionSeguridadDto dto,
        [FromHeader(Name = "X-Forwarded-For")] string? forwardedFor = null)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        string clientIp = IpAddressHelper.ExtractClientIp(
            forwardedFor,
            HttpContext.Connection.RemoteIpAddress);

        int usuarioId = 1;
        var claimValue = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(claimValue, out int parsedId) && parsedId > 0)
        {
            usuarioId = parsedId;
        }

        try
        {
            var resultado = await _configuracionService.ActualizarConfiguracionSeguridadAsync(
                dto,
                clientIp,
                usuarioId);
            return Ok(resultado);
        }
        catch (SubredDiferenteException ex)
        {
            return StatusCode(StatusCodes.Status409Conflict, new
            {
                requiereConfirmacion = true,
                ipClienteDetectada = ex.IpClienteDetectada,
                ipSolicitada = ex.IpSolicitada,
                message = ex.Message
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
