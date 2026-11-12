using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Cine.Application.Dtos;
using Cine.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace cine_back.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TematicasController : ControllerBase
{
    private static readonly JsonSerializerOptions JsonCamelCase = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly IConfiguracionAdminService _configuracionService;
    private readonly ITematicaEventBroadcaster _broadcaster;

    public TematicasController(
        IConfiguracionAdminService configuracionService,
        ITematicaEventBroadcaster broadcaster)
    {
        _configuracionService = configuracionService;
        _broadcaster = broadcaster;
    }

    [Authorize(Roles = "Administrador,Admin")]
    [HttpGet]
    public async Task<ActionResult<List<TematicaSitioDto>>> GetTematicas()
    {
        var lista = await _configuracionService.ObtenerTematicasAsync();
        return Ok(lista);
    }

    [AllowAnonymous]
    [HttpGet("activa")]
    public async Task<ActionResult<TematicaSitioDto>> GetTematicaActiva()
    {
        var activa = await _configuracionService.ObtenerTematicaActivaAsync();
        if (activa == null)
        {
            return NotFound(new { message = "No hay temáticas configuradas en el sistema." });
        }

        return Ok(activa);
    }

    [Authorize(Roles = "Administrador,Admin")]
    [HttpPut("{id:int}/activar")]
    public async Task<ActionResult<TematicaSitioDto>> ActivarTematica(int id)
    {
        try
        {
            var activada = await _configuracionService.ActivarTematicaAsync(id);
            return Ok(activada);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [AllowAnonymous]
    [HttpGet("stream")]
    public async Task StreamTematicaActiva(CancellationToken cancellationToken)
    {
        Response.Headers.ContentType = "text/event-stream; charset=utf-8";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers["X-Accel-Buffering"] = "no";

        var (subscriptionId, reader) = _broadcaster.Subscribe();
        try
        {
            var actual = await _configuracionService.ObtenerTematicaActivaAsync();
            if (actual != null)
            {
                await EscribirEventoSseAsync(actual, cancellationToken);
            }

            while (!cancellationToken.IsCancellationRequested)
            {
                using var heartbeatCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                heartbeatCts.CancelAfter(TimeSpan.FromSeconds(25));

                try
                {
                    if (await reader.WaitToReadAsync(heartbeatCts.Token))
                    {
                        while (reader.TryRead(out var tematica))
                        {
                            await EscribirEventoSseAsync(tematica, cancellationToken);
                        }
                    }
                }
                catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                {
                    await Response.WriteAsync(": keepalive\n\n", cancellationToken);
                    await Response.Body.FlushAsync(cancellationToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Desconexión normal del cliente SSE
        }
        finally
        {
            _broadcaster.Unsubscribe(subscriptionId);
        }
    }

    private async Task EscribirEventoSseAsync(TematicaSitioDto tematica, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(tematica, JsonCamelCase);
        await Response.WriteAsync($"event: theme-updated\ndata: {json}\n\n", cancellationToken);
        await Response.Body.FlushAsync(cancellationToken);
    }
}
