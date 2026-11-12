using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Cine.Application.Dtos;

public class TurnoLaboralDto
{
    public int Id { get; set; }
    public string NombreTurno { get; set; } = string.Empty;
    public string HoraInicio { get; set; } = string.Empty;
    public string HoraFin { get; set; } = string.Empty;
}

public class ConfiguracionSeguridadDto
{
    public int Id { get; set; }
    public int SucursalId { get; set; }
    public string SucursalNombre { get; set; } = string.Empty;
    public string IpDefecto { get; set; } = string.Empty;
    public string IpAutorizada { get; set; } = string.Empty;
    public string HorarioInicioCajeros { get; set; } = "08:00:00";
    public string HorarioFinCajeros { get; set; } = "23:00:00";
    public bool AutorizarExcepcionHorasExtras { get; set; }
    public DateTime FechaUltimaModificacion { get; set; }
    public int UsuarioModificoId { get; set; }
    public string IpClienteDetectada { get; set; } = string.Empty;
    public bool MismaSubredQueCliente { get; set; }
    public List<TurnoLaboralDto> TurnosLaborales { get; set; } = new();
}

public class ActualizarConfiguracionSeguridadDto
{
    public int? SucursalId { get; set; }

    [Required(ErrorMessage = "La dirección IP autorizada es obligatoria.")]
    [StringLength(45, ErrorMessage = "La dirección IP no puede exceder 45 caracteres.")]
    public string IpAutorizada { get; set; } = string.Empty;

    [Required(ErrorMessage = "La hora de inicio de cajeros es obligatoria.")]
    public string HorarioInicioCajeros { get; set; } = "08:00:00";

    [Required(ErrorMessage = "La hora de fin de cajeros es obligatoria.")]
    public string HorarioFinCajeros { get; set; } = "23:00:00";

    public bool AutorizarExcepcionHorasExtras { get; set; }

    /// <summary>
    /// Confirmación explícita requerida por el mecanismo Fail-Safe cuando la IP
    /// solicitada pertenece a una subred distinta a la del Administrador actual.
    /// </summary>
    public bool ConfirmarCambioRed { get; set; }
}

public class TematicaSitioDto
{
    public int Id { get; set; }
    public string NombreTematica { get; set; } = string.Empty;
    public string ClaveIdentificadora { get; set; } = string.Empty;
    public bool EsActiva { get; set; }
    public DateTime? FechaActivacion { get; set; }
    public string RutaArchivoCss { get; set; } = string.Empty;
    public string PaletaColoresJson { get; set; } = "{}";
    public string Tipografia { get; set; } = string.Empty;
    public string Icono { get; set; } = "fa-palette";
}
