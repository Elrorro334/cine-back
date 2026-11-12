using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Cine.Application.Dtos;
using Cine.Application.Exceptions;
using Cine.Application.Helpers;
using Cine.Application.Interfaces;
using Cine.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Cine.Application.Services;

public class ConfiguracionAdminService : IConfiguracionAdminService
{
    private const string DefaultSucursalNombre = "Cine Teatro Xilotzin - Matriz Jilotepec";
    private const string DefaultIp = "192.168.1.100";
    private const string FormatoHora = @"hh\:mm\:ss";
    private static readonly string[] FormatosHoraPermitidos =
    {
        @"hh\:mm\:ss",
        @"h\:mm\:ss",
        @"hh\:mm",
        @"h\:mm"
    };

    private static readonly Dictionary<string, string> IconosPorClave = new(StringComparer.OrdinalIgnoreCase)
    {
        ["theme-classic"] = "fa-clapperboard",
        ["theme-halloween"] = "fa-ghost",
        ["theme-christmas"] = "fa-snowflake",
        ["theme-patrias"] = "fa-flag"
    };

    private readonly ICineDbContext _db;
    private readonly ICineClock _clock;
    private readonly ITematicaEventBroadcaster _broadcaster;

    public ConfiguracionAdminService(
        ICineDbContext db,
        ICineClock clock,
        ITematicaEventBroadcaster broadcaster)
    {
        _db = db;
        _clock = clock;
        _broadcaster = broadcaster;
    }

    public async Task<ConfiguracionSeguridadDto> ObtenerConfiguracionSeguridadAsync(string clientIp)
    {
        string ipDetectada = IpAddressHelper.ResolveLocalAdapterIfLoopback(clientIp);
        var config = await _db.ConfiguracionesSeguridad
            .AsNoTracking()
            .Include(c => c.SucursalNavigation)
            .OrderByDescending(c => c.FechaUltimaModificacion)
            .FirstOrDefaultAsync();

        var turnos = await _db.TurnosLaborales
            .AsNoTracking()
            .OrderBy(t => t.HoraInicio)
            .Select(t => new TurnoLaboralDto
            {
                Id = t.Id,
                NombreTurno = t.NombreTurno,
                HoraInicio = t.HoraInicio.ToString(FormatoHora, CultureInfo.InvariantCulture),
                HoraFin = t.HoraFin.ToString(FormatoHora, CultureInfo.InvariantCulture)
            })
            .ToListAsync();

        if (config == null)
        {
            var sucursal = await _db.Sucursales.AsNoTracking().FirstOrDefaultAsync();
            string ipActiva = sucursal?.IpDefecto ?? DefaultIp;
            return new ConfiguracionSeguridadDto
            {
                Id = 0,
                SucursalId = sucursal?.Id ?? 1,
                SucursalNombre = sucursal?.Nombre ?? DefaultSucursalNombre,
                IpDefecto = ipActiva,
                IpAutorizada = ipActiva,
                HorarioInicioCajeros = "08:00:00",
                HorarioFinCajeros = "23:00:00",
                AutorizarExcepcionHorasExtras = false,
                FechaUltimaModificacion = _clock.GetLocalNow(),
                UsuarioModificoId = 1,
                IpClienteDetectada = ipDetectada,
                MismaSubredQueCliente = IpAddressHelper.IsSameSubnet(ipDetectada, ipActiva),
                TurnosLaborales = turnos
            };
        }

        return MapearConfiguracionDto(config, ipDetectada, turnos);
    }

    public async Task<ConfiguracionSeguridadDto> ActualizarConfiguracionSeguridadAsync(
        ActualizarConfiguracionSeguridadDto dto,
        string clientIp,
        int usuarioModificoId)
    {
        ArgumentNullException.ThrowIfNull(dto);

        if (!IpAddressHelper.TryParseNormalized(dto.IpAutorizada, out var parsedIp) || parsedIp == null)
        {
            throw new ArgumentException("La dirección IP autorizada no tiene un formato IPv4 o IPv6 válido.");
        }

        if (!TryParseHora(dto.HorarioInicioCajeros, out var horaInicio) ||
            !TryParseHora(dto.HorarioFinCajeros, out var horaFin))
        {
            throw new ArgumentException("Los horarios deben tener un formato válido de 24 horas (HH:mm o HH:mm:ss).");
        }

        if (horaInicio == horaFin)
        {
            throw new ArgumentException("La hora de inicio y fin de cajeros no pueden ser iguales.");
        }

        string ipNormalizada = parsedIp.ToString();
        string ipDetectada = IpAddressHelper.ResolveLocalAdapterIfLoopback(clientIp);

        // Procedimiento de Seguridad (Fail-Safe Task):
        // Evitar que el administrador bloquee su propia sesión por un error de captura.
        if (!IpAddressHelper.IsSameSubnet(ipDetectada, ipNormalizada) && !dto.ConfirmarCambioRed)
        {
            throw new SubredDiferenteException(
                ipDetectada,
                ipNormalizada,
                $"Verificación Fail-Safe: La IP '{ipNormalizada}' pertenece a una subred distinta a tu origen actual ('{ipDetectada}'). Confirma explícitamente el cambio para continuar.");
        }

        var sucursal = await ResolverSucursalAsync(dto.SucursalId, ipNormalizada);
        var config = await _db.ConfiguracionesSeguridad
            .Include(c => c.SucursalNavigation)
            .FirstOrDefaultAsync(c => c.SucursalId == sucursal.Id);

        if (config == null)
        {
            config = new ConfiguracionSeguridad
            {
                SucursalId = sucursal.Id,
                SucursalNavigation = sucursal
            };
            _db.ConfiguracionesSeguridad.Add(config);
        }

        config.IpAutorizada = ipNormalizada;
        config.HorarioInicioCajeros = horaInicio;
        config.HorarioFinCajeros = horaFin;
        config.AutorizarExcepcionHorasExtras = dto.AutorizarExcepcionHorasExtras;
        config.FechaUltimaModificacion = _clock.GetLocalNow();
        config.UsuarioModificoId = usuarioModificoId > 0 ? usuarioModificoId : 1;

        await SincronizarRedAutorizadaAsync(ipNormalizada);
        await _db.SaveChangesAsync();

        var turnos = await _db.TurnosLaborales
            .AsNoTracking()
            .OrderBy(t => t.HoraInicio)
            .Select(t => new TurnoLaboralDto
            {
                Id = t.Id,
                NombreTurno = t.NombreTurno,
                HoraInicio = t.HoraInicio.ToString(FormatoHora, CultureInfo.InvariantCulture),
                HoraFin = t.HoraFin.ToString(FormatoHora, CultureInfo.InvariantCulture)
            })
            .ToListAsync();

        return MapearConfiguracionDto(config, ipDetectada, turnos);
    }

    public async Task<List<TematicaSitioDto>> ObtenerTematicasAsync()
    {
        var tematicas = await _db.TematicasSitio
            .AsNoTracking()
            .Include(t => t.PlantillasCss)
            .OrderBy(t => t.Id)
            .ToListAsync();

        return tematicas.Select(MapearTematicaDto).ToList();
    }

    public async Task<TematicaSitioDto?> ObtenerTematicaActivaAsync()
    {
        var activa = await _db.TematicasSitio
            .AsNoTracking()
            .Include(t => t.PlantillasCss)
            .FirstOrDefaultAsync(t => t.EsActiva);

        activa ??= await _db.TematicasSitio
            .AsNoTracking()
            .Include(t => t.PlantillasCss)
            .OrderBy(t => t.Id)
            .FirstOrDefaultAsync();

        return activa == null ? null : MapearTematicaDto(activa);
    }

    public async Task<TematicaSitioDto> ActivarTematicaAsync(int tematicaId)
    {
        var todas = await _db.TematicasSitio
            .Include(t => t.PlantillasCss)
            .ToListAsync();

        var objetivo = todas.FirstOrDefault(t => t.Id == tematicaId);
        if (objetivo == null)
        {
            throw new KeyNotFoundException($"No se encontró la temática con ID {tematicaId}.");
        }

        var ahora = _clock.GetLocalNow();
        foreach (var tematica in todas)
        {
            if (tematica.Id == tematicaId)
            {
                tematica.EsActiva = true;
                tematica.FechaActivacion = ahora;
            }
            else
            {
                tematica.EsActiva = false;
            }
        }

        await _db.SaveChangesAsync();

        var dto = MapearTematicaDto(objetivo);
        await _broadcaster.PublishAsync(dto);
        return dto;
    }

    private async Task<Sucursal> ResolverSucursalAsync(int? sucursalId, string ipPorDefecto)
    {
        Sucursal? sucursal = sucursalId.HasValue
            ? await _db.Sucursales.FirstOrDefaultAsync(s => s.Id == sucursalId.Value)
            : await _db.Sucursales.FirstOrDefaultAsync();

        if (sucursal != null)
        {
            return sucursal;
        }

        sucursal = new Sucursal
        {
            Nombre = DefaultSucursalNombre,
            IpDefecto = ipPorDefecto
        };
        _db.Sucursales.Add(sucursal);
        await _db.SaveChangesAsync();
        return sucursal;
    }

    private async Task SincronizarRedAutorizadaAsync(string ipNormalizada)
    {
        bool yaRegistrada = await _db.RedesAutorizadas
            .AnyAsync(r => r.DireccionIp == ipNormalizada);

        if (!yaRegistrada)
        {
            _db.RedesAutorizadas.Add(new RedesAutorizada
            {
                DireccionIp = ipNormalizada
            });
        }
    }

    private static bool TryParseHora(string valor, out TimeSpan resultado)
    {
        resultado = default;
        if (string.IsNullOrWhiteSpace(valor))
        {
            return false;
        }

        return TimeSpan.TryParseExact(
            valor.Trim(),
            FormatosHoraPermitidos,
            CultureInfo.InvariantCulture,
            out resultado);
    }

    private static ConfiguracionSeguridadDto MapearConfiguracionDto(
        ConfiguracionSeguridad config,
        string ipDetectada,
        List<TurnoLaboralDto> turnos)
    {
        return new ConfiguracionSeguridadDto
        {
            Id = config.Id,
            SucursalId = config.SucursalId,
            SucursalNombre = config.SucursalNavigation?.Nombre ?? DefaultSucursalNombre,
            IpDefecto = config.SucursalNavigation?.IpDefecto ?? config.IpAutorizada,
            IpAutorizada = config.IpAutorizada,
            HorarioInicioCajeros = config.HorarioInicioCajeros.ToString(FormatoHora, CultureInfo.InvariantCulture),
            HorarioFinCajeros = config.HorarioFinCajeros.ToString(FormatoHora, CultureInfo.InvariantCulture),
            AutorizarExcepcionHorasExtras = config.AutorizarExcepcionHorasExtras,
            FechaUltimaModificacion = config.FechaUltimaModificacion,
            UsuarioModificoId = config.UsuarioModificoId,
            IpClienteDetectada = ipDetectada,
            MismaSubredQueCliente = IpAddressHelper.IsSameSubnet(ipDetectada, config.IpAutorizada),
            TurnosLaborales = turnos
        };
    }

    private static TematicaSitioDto MapearTematicaDto(TematicaSitio tematica)
    {
        var plantilla = tematica.PlantillasCss.FirstOrDefault();
        string paleta = plantilla?.PaletaColoresJson ?? "{}";
        string icono = ExtraerIcono(paleta, tematica.ClaveIdentificadora);

        return new TematicaSitioDto
        {
            Id = tematica.Id,
            NombreTematica = tematica.NombreTematica,
            ClaveIdentificadora = tematica.ClaveIdentificadora,
            EsActiva = tematica.EsActiva,
            FechaActivacion = tematica.FechaActivacion,
            RutaArchivoCss = plantilla?.RutaArchivoCss ?? "/css/themes/classic.min.css",
            PaletaColoresJson = paleta,
            Tipografia = plantilla?.Tipografia ?? "Playfair Display & Montserrat",
            Icono = icono
        };
    }

    private static string ExtraerIcono(string paletaJson, string clave)
    {
        try
        {
            using var doc = JsonDocument.Parse(paletaJson);
            if (doc.RootElement.TryGetProperty("icon", out var iconProp) &&
                iconProp.ValueKind == JsonValueKind.String)
            {
                var valor = iconProp.GetString();
                if (!string.IsNullOrWhiteSpace(valor))
                {
                    return valor;
                }
            }
        }
        catch (JsonException)
        {
            // Usar respaldo por clave identificadora si el JSON no incluye icono
        }

        return IconosPorClave.TryGetValue(clave, out var fallback) ? fallback : "fa-palette";
    }
}
